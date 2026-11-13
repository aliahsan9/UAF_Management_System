using UAF.Application.Common.Abstractions;
using UAF.Application.Features.AcademicCatalog.Abstractions;
using UAF.Application.Features.AcademicCatalog.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.AcademicCatalog;

/// <summary>Application use cases for academic calendars and the course catalogue.</summary>
public sealed class AcademicCatalogService
{
    private readonly IAcademicCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AcademicCatalogService(IAcademicCatalogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<AcademicSessionResponse>> GetSessionsAsync(AcademicSessionStatus? status, CancellationToken ct = default)
    {
        ValidateStatus(status);
        var sessions = await _repository.GetSessionsAsync(status, ct);
        return sessions.Select(Map).ToArray();
    }

    public async Task<AcademicSessionResponse> GetSessionAsync(Guid id, CancellationToken ct = default) =>
        Map(await GetRequiredSessionAsync(id, ct));

    public async Task<Guid> CreateSessionAsync(CreateAcademicSessionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = NormalizeCode(request.Code, nameof(request.Code));
        if (await _repository.SessionCodeExistsAsync(code, ct: ct))
            throw new InvalidOperationException($"Academic session code '{code}' is already in use.");

        var session = new AcademicSession(request.Name, code, request.StartDate, request.EndDate);
        await _repository.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return session.Id;
    }

    public async Task UpdateSessionAsync(Guid id, UpdateAcademicSessionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await GetRequiredSessionAsync(id, ct);
        var code = NormalizeCode(request.Code, nameof(request.Code));
        if (await _repository.SessionCodeExistsAsync(code, id, ct))
            throw new InvalidOperationException($"Academic session code '{code}' is already in use.");

        session.UpdateDetails(request.Name, code, request.StartDate, request.EndDate);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task ActivateSessionAsync(Guid id, CancellationToken ct = default) =>
        ChangeSessionAsync(id, x => x.Activate(), ct);

    public Task CompleteSessionAsync(Guid id, CancellationToken ct = default) =>
        ChangeSessionAsync(id, x => x.Complete(), ct);

    public Task CancelSessionAsync(Guid id, CancellationToken ct = default) =>
        ChangeSessionAsync(id, x => x.Cancel(), ct);

    public async Task<IReadOnlyList<CourseResponse>> GetCoursesAsync(Guid? departmentId, bool? isActive, CancellationToken ct = default)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department filter must be a valid ID.", nameof(departmentId));
        var courses = await _repository.GetCoursesAsync(departmentId, isActive, ct);
        return courses.Select(Map).ToArray();
    }

    public async Task<CourseResponse> GetCourseAsync(Guid id, CancellationToken ct = default) =>
        Map(await GetRequiredCourseAsync(id, ct));

    public async Task<Guid> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.DepartmentId == Guid.Empty || !await _repository.DepartmentIsActiveAsync(request.DepartmentId, ct))
            throw new KeyNotFoundException($"Active department '{request.DepartmentId}' was not found.");

        var code = NormalizeCode(request.Code, nameof(request.Code));
        if (await _repository.CourseCodeExistsAsync(request.DepartmentId, code, cancellationToken: ct))
            throw new InvalidOperationException($"Course code '{code}' is already in use in this department.");

        var course = new Course(request.DepartmentId, code, request.Title, request.CreditHours, request.Description);
        await _repository.AddAsync(course, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return course.Id;
    }

    public async Task UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var course = await GetRequiredCourseAsync(id, ct);
        var code = NormalizeCode(request.Code, nameof(request.Code));
        if (await _repository.CourseCodeExistsAsync(course.DepartmentId, code, id, ct))
            throw new InvalidOperationException($"Course code '{code}' is already in use in this department.");

        course.UpdateDetails(code, request.Title, request.CreditHours, request.Description);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task DeactivateCourseAsync(Guid id, CancellationToken ct = default) =>
        ChangeCourseAsync(id, async course =>
        {
            if (await _repository.CourseHasOpenOfferingsAsync(course.Id, ct))
                throw new InvalidOperationException("A course with draft or published offerings cannot be deactivated.");
            course.Deactivate();
        }, ct);

    public Task ReactivateCourseAsync(Guid id, CancellationToken ct = default) =>
        ChangeCourseAsync(id, course => { course.Reactivate(); return Task.CompletedTask; }, ct);

    private async Task ChangeSessionAsync(Guid id, Action<AcademicSession> change, CancellationToken ct)
    {
        var session = await GetRequiredSessionAsync(id, ct);
        change(session);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ChangeCourseAsync(Guid id, Func<Course, Task> change, CancellationToken ct)
    {
        var course = await GetRequiredCourseAsync(id, ct);
        await change(course);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<AcademicSession> GetRequiredSessionAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid academic session ID is required.", nameof(id));
        return await _repository.GetSessionByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Academic session '{id}' was not found.");
    }

    private async Task<Course> GetRequiredCourseAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid course ID is required.", nameof(id));
        return await _repository.GetCourseByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Course '{id}' was not found.");
    }

    private static string NormalizeCode(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Code is required.", parameterName);
        return value.Trim().ToUpperInvariant();
    }

    private static void ValidateStatus(AcademicSessionStatus? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ArgumentOutOfRangeException(nameof(status));
    }

    private static AcademicSessionResponse Map(AcademicSession x) =>
        new(x.Id, x.Name, x.Code, x.StartDate, x.EndDate, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc);

    private static CourseResponse Map(Course x) =>
        new(x.Id, x.DepartmentId, x.Code, x.Title, x.CreditHours, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}