using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Examinations;
using UAF.Application.Features.Examinations.Abstractions;
using UAF.Application.Features.Examinations.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Examinations;

public sealed class ExaminationServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ExaminationService _service;
    private readonly Guid _actorId = Guid.NewGuid();

    public ExaminationServiceTests() => _service = new ExaminationService(_repository, _unitOfWork);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesDraftAndPersists()
    {
        var id = await _service.CreateAsync(CreateRequest(), _actorId, true, false, default);
        var exam = await _repository.GetAsync(id, default);
        Assert.NotNull(exam);
        Assert.Equal("Midterm", exam.Title);
        Assert.Equal(ExaminationStatus.Draft, exam.Status);
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnauthorisedActor()
    {
        _repository.CanAdminister = false;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(CreateRequest(), _actorId, false, false, default));
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_RequiresSessionDateAndUniqueTitle()
    {
        _repository.DateWithinSession = false;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(CreateRequest(), _actorId, true, false, default));
        _repository.DateWithinSession = true;
        await _service.CreateAsync(CreateRequest(), _actorId, true, false, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(CreateRequest(), _actorId, true, false, default));
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyActorAndInvalidDomainMarks()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(CreateRequest(), Guid.Empty, true, false, default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateAsync(CreateRequest() with { MaximumMarks = 0 }, _actorId, true, false, default));
    }

    [Fact]
    public async Task ScheduleAndOpenMarksEntry_EnforceWorkflow()
    {
        var id = await CreateAsync();
        await _service.ScheduleAsync(id, _actorId, true, false, default);
        await _service.OpenMarksEntryAsync(id, _actorId, true, false, default);
        Assert.Equal(ExaminationStatus.MarksEntryOpen, (await _repository.GetAsync(id, default))!.Status);
        Assert.Equal(3, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task GetAndMutation_RejectMissingOrUnauthorizedExamination()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetAsync(Guid.NewGuid(), _actorId, true, false, default));
        var id = await CreateAsync();
        _repository.CanManage = false;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ScheduleAsync(id, _actorId, false, false, default));
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task SaveMarksAsync_ValidatesBatchSizeStudentIdsReasonsAndBoundaries()
    {
        var id = await CreateAsync();
        var student = Guid.NewGuid();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new SaveExaminationMarksRequest(Array.Empty<ExaminationMarkItem>()), default));
        await OpenMarksAsync(id);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(Guid.Empty, 10m) }), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(student, 10m), Mark(student, 12m) }), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(student, 10m) with { Reason = " " } }), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(student, 51m) }), default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(student, 1m) with { IsAbsent = true } }), default));
        await _service.SaveMarksAsync(id, _actorId, true, false,
            new(new[] { Mark(student, 42m) }), default);
        Assert.Single(_repository.SavedMarks);
    }

    [Fact]
    public async Task SaveMarksAsync_RejectsWhenMarksEntryIsNotOpen()
    {
        var id = await CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SaveMarksAsync(id, _actorId, true, false,
                new(new[] { Mark(Guid.NewGuid(), 10m) }), default));
    }

    [Fact]
    public async Task SubmitApprovePublish_EnforceIndependentApprovalAndPublication()
    {
        var id = await CreateAsync();
        await OpenMarksAsync(id);
        await _service.SubmitAsync(id, _actorId, true, false, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(id, _actorId, true, false, default));
        var approver = Guid.NewGuid();
        await _service.ApproveAsync(id, approver, true, false, default);
        await _service.PublishAsync(id, Guid.NewGuid(), true, false, default);
        var result = await _service.GetAsync(id, _actorId, true, false, default);
        Assert.Equal(ExaminationStatus.Published, result.Status);
        Assert.Equal(_actorId, result.SubmittedByUserId);
    }

    [Fact]
    public async Task GetResultsAsync_OnlyReturnsPublishedResults()
    {
        var id = await CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetResultsAsync(id, _actorId, true, false, default));
        await OpenMarksAsync(id);
        await _service.SubmitAsync(id, _actorId, true, false, default);
        await _service.ApproveAsync(id, Guid.NewGuid(), true, false, default);
        await _service.PublishAsync(id, Guid.NewGuid(), true, false, default);
        var results = await _service.GetResultsAsync(id, _actorId, true, false, default);
        Assert.Empty(results);
    }

    [Fact]
    public async Task OfferingList_RequiresManagementAuthorization()
    {
        var offering = _repository.OfferingId;
        _repository.CanManage = false;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetOfferingExaminationsAsync(offering, _actorId, false, false, default));
        _repository.CanManage = true;
        var items = await _service.GetOfferingExaminationsAsync(offering, _actorId, true, false, default);
        Assert.Empty(items);
    }

    private async Task<Guid> CreateAsync() =>
        await _service.CreateAsync(CreateRequest(), _actorId, true, false, default);

    private async Task OpenMarksAsync(Guid id)
    {
        await _service.ScheduleAsync(id, _actorId, true, false, default);
        await _service.OpenMarksEntryAsync(id, _actorId, true, false, default);
    }

    private CreateExaminationRequest CreateRequest() =>
        new(_repository.OfferingId, "Midterm", ExaminationType.Midterm,
            new DateOnly(2026, 11, 20), 50m, 20m, 40m);

    private static ExaminationMarkItem Mark(Guid studentId, decimal marks) =>
        new(studentId, marks, false, null, "Initial marks entry");

    private sealed class FakeRepository : IExaminationRepository
    {
        private readonly Dictionary<Guid, Examination> _examinations = new();
        public Guid OfferingId { get; } = Guid.NewGuid();
        public bool CanAdminister { get; set; } = true;
        public bool CanManage { get; set; } = true;
        public bool DateWithinSession { get; set; } = true;
        public IReadOnlyCollection<ExaminationMarkItem> SavedMarks { get; private set; } = Array.Empty<ExaminationMarkItem>();

        public Task<bool> CanManageOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct, bool allowClosed = false) =>
            Task.FromResult(offeringId == OfferingId && (privileged || departmentAdmin || CanManage));
        public Task<bool> CanAdministerOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct) =>
            Task.FromResult(CanAdminister && offeringId == OfferingId);
        public Task AddAsync(Examination examination, CancellationToken ct)
        {
            _examinations.Add(examination.Id, examination);
            return Task.CompletedTask;
        }
        public Task<Examination?> GetAsync(Guid id, CancellationToken ct)
        {
            _examinations.TryGetValue(id, out var exam);
            return Task.FromResult(exam);
        }
        public Task<bool> ExistsAsync(Guid offeringId, string title, CancellationToken ct) =>
            Task.FromResult(_examinations.Values.Any(x => x.CourseOfferingId == offeringId &&
                string.Equals(x.Title, title.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<bool> IsScheduledDateWithinSessionAsync(Guid offeringId, DateOnly date, CancellationToken ct) =>
            Task.FromResult(DateWithinSession);
        public Task SaveMarksAsync(Guid id, Guid actorId, IReadOnlyCollection<ExaminationMarkItem> marks, CancellationToken ct)
        {
            SavedMarks = marks.ToArray();
            return Task.CompletedTask;
        }
        public Task SubmitAsync(Guid id, Guid actorId, CancellationToken ct)
        {
            var exam = _examinations[id];
            exam.Submit(actorId, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }
        public Task ApproveAsync(Guid id, Guid actorId, CancellationToken ct)
        {
            _examinations[id].Approve(actorId, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }
        public Task PublishAsync(Guid id, Guid actorId, CancellationToken ct)
        {
            _examinations[id].Publish(actorId, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<ExaminationResultView>> GetResultsAsync(Guid id, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<ExaminationResultView>)Array.Empty<ExaminationResultView>());
        public Task<IReadOnlyList<ExaminationListItem>> GetOfferingExaminationsAsync(Guid offeringId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<ExaminationListItem>)Array.Empty<ExaminationListItem>());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
