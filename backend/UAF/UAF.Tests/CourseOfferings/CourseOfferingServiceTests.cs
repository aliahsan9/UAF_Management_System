using UAF.Application.Common.Abstractions;
using UAF.Application.Features.CourseOfferings;
using UAF.Application.Features.CourseOfferings.Abstractions;
using UAF.Application.Features.CourseOfferings.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.CourseOfferings;

public sealed class CourseOfferingServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly CourseOfferingService _service;
    public CourseOfferingServiceTests() => _service = new CourseOfferingService(_repository, new FakeUnitOfWork());

    [Fact]
    public async Task CreateOffering_RequiresActiveCourseAndSchedulableSession()
    {
        _repository.CourseActive = false;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateOfferingAsync(Request()));
        _repository.CourseActive = true;
        _repository.SessionSchedulable = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateOfferingAsync(Request()));
    }

    [Fact]
    public async Task CreateOffering_NormalizesSectionAndStartsAsDraft()
    {
        var id = await _service.CreateOfferingAsync(Request());
        var offering = await _repository.GetOfferingByIdAsync(id);
        Assert.NotNull(offering);
        Assert.Equal("A", offering.Section);
        Assert.Equal(CourseOfferingStatus.Draft, offering.Status);
    }

    [Fact]
    public async Task Publish_OnlyAllowsDraftOffering()
    {
        var id = await _service.CreateOfferingAsync(Request());
        await _service.PublishAsync(id);
        Assert.Equal(CourseOfferingStatus.Published, (await _repository.GetOfferingByIdAsync(id))!.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PublishAsync(id));
    }

    [Fact]
    public async Task CreateOffering_RejectsDuplicateSection()
    {
        await _service.CreateOfferingAsync(Request());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateOfferingAsync(Request()));
    }

    [Fact]
    public async Task Enrollment_RequiresPublishedOfferingAndActiveStudent()
    {
        var id = await _service.CreateOfferingAsync(Request());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollStudentAsync(_repository.StudentId, id, DateOnly.FromDateTime(DateTime.UtcNow)));
        await _service.PublishAsync(id);
        _repository.StudentActive = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollStudentAsync(_repository.StudentId, id, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public async Task Enrollment_PreventsDuplicateAndEnforcesCapacity()
    {
        var id = await _service.CreateOfferingAsync(Request(capacity: 1));
        await _service.PublishAsync(id);
        await _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13)));

        var secondStudent = Guid.NewGuid();
        _repository.EligibleStudents.Add(secondStudent);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollStudentAsync(secondStudent, id, new(2026, 11, 13)));
    }

    [Fact]
    public async Task Capacity_CannotBeReducedBelowCurrentEnrollment()
    {
        var id = await _service.CreateOfferingAsync(Request(capacity: 3));
        await _service.PublishAsync(id);
        await _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateOfferingAsync(id, new(0, null)));
    }

    [Fact]
    public async Task ConcurrentEnrollmentRequests_NeverExceedOfferingCapacity()
    {
        var id = await _service.CreateOfferingAsync(Request(capacity: 2));
        await _service.PublishAsync(id);
        var students = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var studentId in students)
            _repository.EligibleStudents.Add(studentId);

        var attempts = students.Select(async studentId =>
        {
            try
            {
                await _service.EnrollStudentAsync(studentId, id, new(2026, 11, 13));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(attempts);

        Assert.Equal(2, results.Count(x => x));
        Assert.Equal(2, await _repository.GetActiveEnrollmentCountAsync(id));
    }

    [Fact]
    public async Task StudentCanReEnrollAfterDroppingPreviousRegistration()
    {
        var id = await _service.CreateOfferingAsync(Request(capacity: 1));
        await _service.PublishAsync(id);
        var firstEnrollmentId = await _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13));
        await _service.DropEnrollmentAsync(firstEnrollmentId);

        var secondEnrollmentId = await _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13));

        Assert.NotEqual(firstEnrollmentId, secondEnrollmentId);
        Assert.Equal(1, await _repository.GetActiveEnrollmentCountAsync(id));
    }

    [Fact]
    public async Task DroppingEnrollmentReleasesSeat()
    {
        var id = await _service.CreateOfferingAsync(Request(capacity: 1));
        await _service.PublishAsync(id);
        var enrollmentId = await _service.EnrollStudentAsync(_repository.StudentId, id, new(2026, 11, 13));
        await _service.DropEnrollmentAsync(enrollmentId);
        var secondStudent = Guid.NewGuid();
        _repository.EligibleStudents.Add(secondStudent);
        await _service.EnrollStudentAsync(secondStudent, id, new(2026, 11, 13));
        Assert.Equal(1, await _repository.GetActiveEnrollmentCountAsync(id));
    }

    private CreateCourseOfferingRequest Request(int capacity = 30) =>
        new(_repository.CourseId, _repository.SessionId, " a ", capacity, null);

    private sealed class FakeRepository : ICourseOfferingRepository
    {
        private readonly Dictionary<Guid, CourseOffering> _offerings = new();
        private readonly Dictionary<Guid, CourseEnrollment> _enrollments = new();
        private readonly SemaphoreSlim _enrollmentGate = new(1, 1);
        public Guid CourseId { get; } = Guid.NewGuid();
        public Guid SessionId { get; } = Guid.NewGuid();
        public Guid StudentId { get; } = Guid.NewGuid();
        public bool CourseActive { get; set; } = true;
        public bool SessionSchedulable { get; set; } = true;
        public bool StudentActive { get; set; } = true;
        public HashSet<Guid> EligibleStudents { get; } = new();

        public Task<IReadOnlyList<CourseOffering>> GetOfferingsAsync(Guid? sessionId, Guid? courseId, CourseOfferingStatus? status, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<CourseOffering>)_offerings.Values.Where(x =>
                (!sessionId.HasValue || x.AcademicSessionId == sessionId) && (!courseId.HasValue || x.CourseId == courseId) &&
                (!status.HasValue || x.Status == status)).ToArray());
        public Task<CourseOffering?> GetOfferingByIdAsync(Guid id, CancellationToken ct = default)
        { _offerings.TryGetValue(id, out var x); return Task.FromResult(x); }
        public Task<bool> CourseIsActiveAsync(Guid id, CancellationToken ct = default) => Task.FromResult(CourseActive && id == CourseId);
        public Task<bool> AcademicSessionAllowsSchedulingAsync(Guid id, CancellationToken ct = default) => Task.FromResult(SessionSchedulable && id == SessionId);
        public Task<bool> OfferingSectionExistsAsync(Guid courseId, Guid sessionId, string section, Guid? excludingId = null, CancellationToken ct = default) =>
            Task.FromResult(_offerings.Values.Any(x => x.CourseId == courseId && x.AcademicSessionId == sessionId && x.Section == section && x.Id != excludingId));
        public Task<int> GetActiveEnrollmentCountAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_enrollments.Values.Count(x => x.CourseOfferingId == id && x.Status == EnrollmentStatus.Enrolled));
        public Task AddOfferingAsync(CourseOffering offering, CancellationToken ct = default) { _offerings.Add(offering.Id, offering); return Task.CompletedTask; }
        public Task UpdateOfferingAsync(Guid offeringId, int capacity, Guid? instructorEmployeeId, CancellationToken ct = default)
        {
            var offering = _offerings[offeringId];
            var enrolled = _enrollments.Values.Count(x => x.CourseOfferingId == offeringId && x.Status == EnrollmentStatus.Enrolled);
            if (capacity < enrolled) throw new InvalidOperationException();
            offering.UpdateCapacity(capacity);
            offering.AssignInstructor(instructorEmployeeId);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<CourseEnrollment>> GetEnrollmentsAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<CourseEnrollment>)_enrollments.Values.Where(x => x.CourseOfferingId == id).ToArray());
        public Task<CourseEnrollment?> GetEnrollmentAsync(Guid id, CancellationToken ct = default)
        { _enrollments.TryGetValue(id, out var x); return Task.FromResult(x); }
        public async Task<CourseEnrollment> EnrollAsync(Guid studentId, Guid offeringId, DateOnly enrolledOn, CancellationToken ct = default)
        {
            await _enrollmentGate.WaitAsync(ct);
            try
            {
                var offering = _offerings[offeringId];
                if (offering.Status != CourseOfferingStatus.Published) throw new InvalidOperationException();
                if (!((StudentActive && studentId == StudentId) || EligibleStudents.Contains(studentId)))
                    throw new InvalidOperationException();
                if (_enrollments.Values.Any(x => x.StudentId == studentId && x.CourseOfferingId == offeringId &&
                    (x.Status == EnrollmentStatus.Enrolled || x.Status == EnrollmentStatus.Completed))) throw new InvalidOperationException();
                if (_enrollments.Values.Count(x => x.CourseOfferingId == offeringId && x.Status == EnrollmentStatus.Enrolled) >= offering.Capacity)
                    throw new InvalidOperationException();
                var enrollment = new CourseEnrollment(studentId, offeringId, enrolledOn);
                _enrollments.Add(enrollment.Id, enrollment);
                return enrollment;
            }
            finally
            {
                _enrollmentGate.Release();
            }
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}