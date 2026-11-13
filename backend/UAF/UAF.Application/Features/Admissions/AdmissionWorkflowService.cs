using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions;

public sealed class AdmissionWorkflowService
{
    private readonly IAdmissionWorkflowRepository _repository;
    public AdmissionWorkflowService(IAdmissionWorkflowRepository repository) =>
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task StartReviewAsync(Guid applicationId, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureIds(applicationId, actorUserId);
        return _repository.StartReviewAsync(applicationId, actorUserId, DateTimeOffset.UtcNow, ct);
    }

    public Task RecordDecisionAsync(Guid applicationId, Guid actorUserId, AdmissionStatus decision,
        string? remarks, CancellationToken ct = default)
    {
        EnsureIds(applicationId, actorUserId);
        if (decision is not AdmissionStatus.Accepted and not AdmissionStatus.Rejected and not AdmissionStatus.Waitlisted)
            throw new ArgumentOutOfRangeException(nameof(decision));
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ArgumentException("Decision remarks are required.", nameof(remarks));
        return _repository.RecordDecisionAsync(applicationId, actorUserId, decision, remarks, DateTimeOffset.UtcNow, ct);
    }

    public Task ConfigureRegistrationSeriesAsync(ConfigureRegistrationSeriesRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorUserId == Guid.Empty) throw new ArgumentException("Authenticated registrar is required.", nameof(actorUserId));
        if (request.AcademicProgramId == Guid.Empty) throw new ArgumentException("Academic program is required.", nameof(request));
        if (request.AdmissionYear < 2000 || request.AdmissionYear > 9999) throw new ArgumentOutOfRangeException(nameof(request.AdmissionYear));
        if (request.NextNumber < 1) throw new ArgumentOutOfRangeException(nameof(request.NextNumber));
        return _repository.ConfigureRegistrationSeriesAsync(
            request.AcademicProgramId, request.AdmissionYear, request.Prefix, request.NextNumber, actorUserId, ct);
    }

    public async Task<ConvertedStudentResponse> ConvertAcceptedApplicationAsync(Guid applicationId, Guid actorUserId, CancellationToken ct = default)
    {
        EnsureIds(applicationId, actorUserId);
        var student = await _repository.ConvertAcceptedApplicationAsync(applicationId, actorUserId, DateTimeOffset.UtcNow, ct);
        return new ConvertedStudentResponse(student.Id, student.AdmissionApplicationId!.Value,
            student.RegistrationNumber, student.FullName, student.Email, student.AcademicProgramId,
            student.EnrollmentDate, student.IdentityUserId, student.RegisteredByUserId!.Value,
            student.RegisteredAtUtc!.Value);
    }

    private static void EnsureIds(Guid applicationId, Guid actorUserId)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("A valid application ID is required.", nameof(applicationId));
        if (actorUserId == Guid.Empty) throw new ArgumentException("Authenticated staff actor is required.", nameof(actorUserId));
    }

    public async Task<IReadOnlyList<AdmissionDecisionAuditResponse>> GetDecisionHistoryAsync(Guid applicationId, CancellationToken ct = default)
    {
        var history = await _repository.GetDecisionHistoryAsync(applicationId, ct);
        return history.Select(x => x.ToResponse()).ToArray();
    }
}
