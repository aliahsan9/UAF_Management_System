using UAF.Application.Common.Abstractions;
using UAF.Application.Features.CourseOfferings.Abstractions;
using UAF.Application.Features.CourseOfferings.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.CourseOfferings;

/// <summary>Use cases for scheduling course sections and managing student registrations.</summary>
public sealed class CourseOfferingService
{
    private readonly ICourseOfferingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CourseOfferingService(ICourseOfferingRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<CourseOfferingResponse>> GetOfferingsAsync(Guid? sessionId, Guid? courseId, CourseOfferingStatus? status, CancellationToken ct = default)
    {
        ValidateOptionalId(sessionId, nameof(sessionId));
        ValidateOptionalId(courseId, nameof(courseId));
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ArgumentOutOfRangeException(nameof(status));
        var offerings = await _repository.GetOfferingsAsync(sessionId, courseId, status, ct);
        var result = new List<CourseOfferingResponse>(offerings.Count);
        foreach (var offering in offerings)
            result.Add(await MapAsync(offering, ct));
        return result;
    }

    public async Task<CourseOfferingResponse> GetOfferingAsync(Guid id, CancellationToken ct = default) =>
        await MapAsync(await GetRequiredOfferingAsync(id, ct), ct);

    public async Task<Guid> CreateOfferingAsync(CreateCourseOfferingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CourseId == Guid.Empty || !await _repository.CourseIsActiveAsync(request.CourseId, ct))
            throw new KeyNotFoundException($"Active course '{request.CourseId}' was not found.");
        if (request.AcademicSessionId == Guid.Empty ||
            !await _repository.AcademicSessionAllowsSchedulingAsync(request.AcademicSessionId, ct))
            throw new InvalidOperationException("Course offerings can only be scheduled in a planned or active academic session.");

        var section = NormalizeSection(request.Section);
        if (await _repository.OfferingSectionExistsAsync(request.CourseId, request.AcademicSessionId, section, cancellationToken: ct))
            throw new InvalidOperationException($"Section '{section}' already exists for this course and academic session.");

        var offering = new CourseOffering(request.CourseId, request.AcademicSessionId, section, request.Capacity, request.InstructorEmployeeId);
        await _repository.AddOfferingAsync(offering, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return offering.Id;
    }

    public async Task UpdateOfferingAsync(Guid id, UpdateCourseOfferingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id == Guid.Empty) throw new ArgumentException("A valid course offering ID is required.", nameof(id));
        // Capacity changes share a serializable transaction with enrollment inserts.
        await _repository.UpdateOfferingAsync(id, request.Capacity, request.InstructorEmployeeId, ct);
    }

    public async Task PublishAsync(Guid id, CancellationToken ct = default)
    {
        var offering = await GetRequiredOfferingAsync(id, ct);
        if (!await _repository.CourseIsActiveAsync(offering.CourseId, ct))
            throw new InvalidOperationException("An offering cannot be published for an inactive course.");
        if (!await _repository.AcademicSessionAllowsSchedulingAsync(offering.AcademicSessionId, ct))
            throw new InvalidOperationException("An offering cannot be published for a completed or cancelled academic session.");
        offering.Publish();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task CloseAsync(Guid id, CancellationToken ct = default) =>
        ChangeOfferingAsync(id, x => x.Close(), ct);

    public Task CancelAsync(Guid id, CancellationToken ct = default) =>
        ChangeOfferingAsync(id, x => x.Cancel(), ct);

    public async Task<IReadOnlyList<CourseEnrollmentResponse>> GetEnrollmentsAsync(Guid offeringId, CancellationToken ct = default)
    {
        _ = await GetRequiredOfferingAsync(offeringId, ct);
        var items = await _repository.GetEnrollmentsAsync(offeringId, ct);
        return items.Select(Map).ToArray();
    }

    public async Task<Guid> EnrollStudentAsync(Guid studentId, Guid offeringId, DateOnly enrolledOn, CancellationToken ct = default)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("A valid student ID is required.", nameof(studentId));
        if (offeringId == Guid.Empty) throw new ArgumentException("A valid course offering ID is required.", nameof(offeringId));

        // The repository validates offering status and student eligibility inside the same
        // serializable transaction as the capacity check and insert.
        var enrollment = await _repository.EnrollAsync(studentId, offeringId, enrolledOn, ct);
        return enrollment.Id;
    }

    public async Task DropEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await GetRequiredEnrollmentAsync(enrollmentId, ct);
        enrollment.Drop();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task WithdrawEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await GetRequiredEnrollmentAsync(enrollmentId, ct);
        enrollment.Withdraw();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CompleteEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await GetRequiredEnrollmentAsync(enrollmentId, ct);
        enrollment.Complete();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ChangeOfferingAsync(Guid id, Action<CourseOffering> change, CancellationToken ct)
    {
        var offering = await GetRequiredOfferingAsync(id, ct);
        change(offering);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<CourseOffering> GetRequiredOfferingAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid course offering ID is required.", nameof(id));
        return await _repository.GetOfferingByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Course offering '{id}' was not found.");
    }

    private async Task<CourseEnrollment> GetRequiredEnrollmentAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid enrollment ID is required.", nameof(id));
        return await _repository.GetEnrollmentAsync(id, ct)
            ?? throw new KeyNotFoundException($"Course enrollment '{id}' was not found.");
    }

    private async Task<CourseOfferingResponse> MapAsync(CourseOffering offering, CancellationToken ct) =>
        new(offering.Id, offering.CourseId, offering.AcademicSessionId, offering.Section, offering.Capacity,
            offering.InstructorEmployeeId, offering.Status,
            await _repository.GetActiveEnrollmentCountAsync(offering.Id, ct));

    private static CourseEnrollmentResponse Map(CourseEnrollment x) =>
        new(x.Id, x.StudentId, x.CourseOfferingId, x.EnrolledOn, x.Status);

    private static string NormalizeSection(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Section is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 20) throw new ArgumentOutOfRangeException(nameof(value), "Section cannot exceed 20 characters.");
        return normalized;
    }

    private static void ValidateOptionalId(Guid? id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Filter ID must be valid.", name);
    }
}