using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Examinations.Abstractions;
using UAF.Application.Features.Examinations.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Examinations;
public sealed class ExaminationService
{
    private readonly IExaminationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public ExaminationService(IExaminationRepository repository, IUnitOfWork unitOfWork) { _repository = repository; _unitOfWork = unitOfWork; }
    public async Task<Guid> CreateAsync(CreateExaminationRequest request, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        if (!await _repository.CanAdministerOfferingAsync(request.CourseOfferingId, actorId, privileged, departmentAdmin, ct))
            throw new UnauthorizedAccessException("You cannot schedule examinations for this course offering.");
        if (!await _repository.IsScheduledDateWithinSessionAsync(request.CourseOfferingId, request.ScheduledDate, ct))
            throw new ArgumentException("Examination date must fall within the course offering's academic session.");
        if (await _repository.ExistsAsync(request.CourseOfferingId, request.Title, ct))
            throw new InvalidOperationException("An examination with this title already exists for this course offering.");
        var exam = new Examination(request.CourseOfferingId, request.Title, request.Type, request.ScheduledDate, request.MaximumMarks, request.PassingMarks, request.WeightPercentage);
        await _repository.AddAsync(exam, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return exam.Id;
    }
    public async Task ScheduleAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct);
        exam.Schedule();
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task OpenMarksEntryAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct);
        exam.OpenMarksEntry();
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task SaveMarksAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, SaveExaminationMarksRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Marks is null || request.Marks.Count == 0 || request.Marks.Count > 500) throw new ArgumentException("Provide between 1 and 500 student marks.");
        if (request.Marks.Select(x => x.StudentId).Any(x => x == Guid.Empty) || request.Marks.Select(x => x.StudentId).Distinct().Count() != request.Marks.Count)
            throw new ArgumentException("Student IDs must be valid and unique within the request.");
        if (request.Marks.Any(x => string.IsNullOrWhiteSpace(x.Reason))) throw new ArgumentException("Every marks entry requires an audit reason.");
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct);
        if (exam.Status != UAF.Domain.Enums.ExaminationStatus.MarksEntryOpen) throw new InvalidOperationException("Marks can only be entered while marks entry is open.");
        if (request.Marks.Any(x => x.MarksObtained < 0 || x.MarksObtained > exam.MaximumMarks || (x.IsAbsent && x.MarksObtained != 0)))
            throw new ArgumentException("Marks must be within the examination maximum; absent students must receive zero.");
        await _repository.SaveMarksAsync(id, actorId, request.Marks, ct);
    }
    public async Task SubmitAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct);
        await _repository.SubmitAsync(id, actorId, ct);
    }
    public async Task ApproveAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct, allowClosed: true);
        exam.Approve(actorId, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task PublishAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct, allowClosed: true);
        exam.Publish(actorId, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task<ExaminationResponse> GetAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct, allowClosed: true);
        return ToResponse(exam);
    }
    public async Task<IReadOnlyList<ExaminationListItem>> GetOfferingExaminationsAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        if (!await _repository.CanManageOfferingAsync(offeringId, actorId, privileged, departmentAdmin, ct, allowClosed: true))
            throw new UnauthorizedAccessException("You cannot view examinations for this offering.");
        return await _repository.GetOfferingExaminationsAsync(offeringId, ct);
    }
    public async Task<IReadOnlyList<ExaminationResultView>> GetResultsAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var exam = await GetAuthorizedAsync(id, actorId, privileged, departmentAdmin, ct, allowClosed: true);
        if (exam.Status != UAF.Domain.Enums.ExaminationStatus.Published)
            throw new InvalidOperationException("Results are available only after publication.");
        return await _repository.GetResultsAsync(id, ct);
    }
    private async Task<Examination> GetAuthorizedAsync(Guid id, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct, bool allowClosed = false)
    {
        var exam = await _repository.GetAsync(id, ct) ?? throw new KeyNotFoundException("Examination not found.");
        if (!await _repository.CanManageOfferingAsync(exam.CourseOfferingId, actorId, privileged, departmentAdmin, ct, allowClosed))
            throw new UnauthorizedAccessException("You cannot access this examination.");
        return exam;
    }
    private static ExaminationResponse ToResponse(Examination x) => new(x.Id, x.CourseOfferingId, x.Title, x.Type, x.ScheduledDate,
        x.MaximumMarks, x.PassingMarks, x.Status, x.SubmittedByUserId, x.SubmittedAtUtc, x.ApprovedByUserId, x.ApprovedAtUtc, x.PublishedByUserId, x.PublishedAtUtc);
}
