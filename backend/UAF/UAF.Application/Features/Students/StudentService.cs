using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Students.Abstractions;
using UAF.Application.Features.Students.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Students;

/// <summary>
/// Implements administrative student-management use cases.
/// Student account ownership is intentionally not inferred from email addresses.
/// </summary>
public sealed class StudentService
{
    private const int MaximumPageSize = 100;
    private readonly IStudentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PagedStudentsResponse> GetPageAsync(
        int page, int pageSize, Guid? academicProgramId = null, StudentStatus? status = null,
        string? search = null, string sortBy = "registrationNumber", string sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (page > int.MaxValue / Math.Max(pageSize, 1))
            throw new ArgumentOutOfRangeException(nameof(page), "Requested page is too large.");
        if (pageSize is < 1 or > MaximumPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size must be between 1 and {MaximumPageSize}.");
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("Academic program filter must be a valid ID.", nameof(academicProgramId));
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ArgumentOutOfRangeException(nameof(status));

        var normalizedSort = sortBy.Trim().ToLowerInvariant();
        if (normalizedSort is not ("registrationnumber" or "fullname" or "enrollmentdate"))
            throw new ArgumentException("SortBy must be registrationNumber, fullName, or enrollmentDate.", nameof(sortBy));
        var normalizedDirection = sortDirection.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
            throw new ArgumentException("SortDirection must be asc or desc.", nameof(sortDirection));
        var (students, totalCount) = await _repository.GetPageAsync(
            page, pageSize, academicProgramId, status, search, normalizedSort, normalizedDirection, ct);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedStudentsResponse(
            students.Select(Map).ToArray(), page, pageSize, totalCount, totalPages);
    }

    public async Task<StudentResponse> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Map(await GetRequiredAsync(id, ct));

    public async Task<Guid> CreateAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var registrationNumber = NormalizeRegistrationNumber(request.RegistrationNumber);

        if (request.AcademicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be selected.", nameof(request));

        if (!await _repository.AcademicProgramIsActiveAsync(request.AcademicProgramId, ct))
            throw new KeyNotFoundException($"Active academic program '{request.AcademicProgramId}' was not found.");

        if (await _repository.RegistrationNumberExistsAsync(registrationNumber, cancellationToken: ct))
            throw new InvalidOperationException($"Registration number '{registrationNumber}' is already in use.");

        var student = new Student(
            registrationNumber,
            request.FullName,
            request.Email,
            request.AcademicProgramId,
            request.EnrollmentDate,
            request.PhoneNumber);

        await _repository.AddAsync(student, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return student.Id;
    }

    public async Task UpdateProfileAsync(Guid id, UpdateStudentProfileRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var student = await GetRequiredAsync(id, ct);
        student.UpdateProfile(request.FullName, request.Email, request.PhoneNumber);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeProgramAsync(Guid id, ChangeStudentProgramRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var student = await GetRequiredAsync(id, ct);

        if (request.AcademicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be selected.", nameof(request));
        if (!await _repository.AcademicProgramIsActiveAsync(request.AcademicProgramId, ct))
            throw new KeyNotFoundException($"Active academic program '{request.AcademicProgramId}' was not found.");

        student.ChangeAcademicProgram(request.AcademicProgramId);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task SuspendAsync(Guid id, CancellationToken ct = default) =>
        ChangeStatusAsync(id, student => student.Suspend(), ct);

    public Task ReactivateAsync(Guid id, CancellationToken ct = default) =>
        ChangeStatusAsync(id, student => student.Reactivate(), ct);

    public Task GraduateAsync(Guid id, CancellationToken ct = default) =>
        ChangeStatusAsync(id, student => student.MarkAsGraduated(), ct);

    public Task WithdrawAsync(Guid id, CancellationToken ct = default) =>
        ChangeStatusAsync(id, student => student.Withdraw(), ct);

    private async Task ChangeStatusAsync(Guid id, Action<Student> change, CancellationToken ct)
    {
        var student = await GetRequiredAsync(id, ct);
        change(student);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Student> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid student ID is required.", nameof(id));
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Student '{id}' was not found.");
    }

    private static string NormalizeRegistrationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Student registration number is required.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }

    private static StudentResponse Map(Student student) =>
        new(student.Id, student.RegistrationNumber, student.FullName, student.Email,
            student.PhoneNumber, student.AcademicProgramId, student.EnrollmentDate,
            student.Status, student.CreatedAtUtc, student.UpdatedAtUtc);
}