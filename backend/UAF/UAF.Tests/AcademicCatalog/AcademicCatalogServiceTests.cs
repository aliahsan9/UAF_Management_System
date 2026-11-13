using UAF.Application.Common.Abstractions;
using UAF.Application.Features.AcademicCatalog;
using UAF.Application.Features.AcademicCatalog.Abstractions;
using UAF.Application.Features.AcademicCatalog.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.AcademicCatalog;

public sealed class AcademicCatalogServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AcademicCatalogService _service;

    public AcademicCatalogServiceTests() => _service = new AcademicCatalogService(_repository, _unitOfWork);

    [Fact]
    public async Task CreateSessionAsync_CreatesPlannedSessionAndNormalizesCode()
    {
        var id = await _service.CreateSessionAsync(new("Fall 2026", " fall-2026 ", new(2026, 9, 1), new(2027, 1, 31)));
        var session = await _repository.GetSessionByIdAsync(id);
        Assert.NotNull(session);
        Assert.Equal("FALL-2026", session.Code);
        Assert.Equal(AcademicSessionStatus.Planned, session.Status);
    }

    [Fact]
    public async Task CreateSessionAsync_DuplicateCode_RejectsRequest()
    {
        var request = new CreateAcademicSessionRequest("Fall 2026", "FALL-2026", new(2026, 9, 1), new(2027, 1, 31));
        await _service.CreateSessionAsync(request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateSessionAsync(request));
    }

    [Fact]
    public async Task SessionLifecycle_RejectsCompletingPlannedSession()
    {
        var id = await _service.CreateSessionAsync(new("Fall", "FALL", new(2026, 9, 1), new(2027, 1, 31)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteSessionAsync(id));
        await _service.ActivateSessionAsync(id);
        await _service.CompleteSessionAsync(id);
        Assert.Equal(AcademicSessionStatus.Completed, (await _repository.GetSessionByIdAsync(id))!.Status);
    }

    [Fact]
    public async Task CreateCourse_RequiresActiveDepartmentAndUniqueDepartmentCode()
    {
        _repository.DepartmentActive = false;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateCourseAsync(CourseRequest()));
        _repository.DepartmentActive = true;
        await _service.CreateCourseAsync(CourseRequest());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCourseAsync(CourseRequest()));
    }

    [Fact]
    public async Task DeactivateCourse_WithDraftOrPublishedOffering_IsRejected()
    {
        var id = await _service.CreateCourseAsync(CourseRequest());
        _repository.HasOpenOffering = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeactivateCourseAsync(id));
        Assert.True((await _repository.GetCourseByIdAsync(id))!.IsActive);
    }

    [Fact]
    public async Task CourseLifecycle_DeactivateAndReactivate()
    {
        var id = await _service.CreateCourseAsync(CourseRequest());
        await _service.DeactivateCourseAsync(id);
        Assert.False((await _repository.GetCourseByIdAsync(id))!.IsActive);
        await _service.ReactivateCourseAsync(id);
        Assert.True((await _repository.GetCourseByIdAsync(id))!.IsActive);
    }

    [Fact]
    public async Task CreateCourse_InvalidCreditHours_IsRejectedByDomain()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateCourseAsync(CourseRequest() with { CreditHours = 0 }));
    }

    private CreateCourseRequest CourseRequest() =>
        new(_repository.DepartmentId, " cs-101 ", "Introduction to Computing", 3m, "Foundational course.");

    private sealed class FakeRepository : IAcademicCatalogRepository
    {
        private readonly Dictionary<Guid, AcademicSession> _sessions = new();
        private readonly Dictionary<Guid, Course> _courses = new();
        public Guid DepartmentId { get; } = Guid.NewGuid();
        public bool DepartmentActive { get; set; } = true;
        public bool HasOpenOffering { get; set; }

        public Task<IReadOnlyList<AcademicSession>> GetSessionsAsync(AcademicSessionStatus? status, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<AcademicSession>)_sessions.Values.Where(x => !status.HasValue || x.Status == status).ToArray());
        public Task<AcademicSession?> GetSessionByIdAsync(Guid id, CancellationToken ct = default)
        {
            _sessions.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }
        public Task<bool> SessionCodeExistsAsync(string code, Guid? excludingId = null, CancellationToken ct = default) =>
            Task.FromResult(_sessions.Values.Any(x => x.Code == code && x.Id != excludingId));
        public Task AddAsync(AcademicSession session, CancellationToken ct = default) { _sessions.Add(session.Id, session); return Task.CompletedTask; }
        public Task<IReadOnlyList<Course>> GetCoursesAsync(Guid? departmentId, bool? isActive, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Course>)_courses.Values.Where(x =>
                (!departmentId.HasValue || x.DepartmentId == departmentId) && (!isActive.HasValue || x.IsActive == isActive)).ToArray());
        public Task<Course?> GetCourseByIdAsync(Guid id, CancellationToken ct = default)
        {
            _courses.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }
        public Task<bool> DepartmentIsActiveAsync(Guid departmentId, CancellationToken ct = default) =>
            Task.FromResult(DepartmentActive && departmentId == DepartmentId);
        public Task<bool> CourseCodeExistsAsync(Guid departmentId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
            Task.FromResult(_courses.Values.Any(x => x.DepartmentId == departmentId && x.Code == code && x.Id != excludingId));
        public Task<bool> CourseHasOpenOfferingsAsync(Guid courseId, CancellationToken ct = default) =>
            Task.FromResult(HasOpenOffering);
        public Task AddAsync(Course course, CancellationToken ct = default) { _courses.Add(course.Id, course); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}