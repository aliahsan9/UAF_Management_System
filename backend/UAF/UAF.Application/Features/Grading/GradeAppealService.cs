using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Grading;
public sealed class GradeAppealService
{
    private readonly IGradeAppealRepository _repository;
    public GradeAppealService(IGradeAppealRepository repository) => _repository = repository;
    public async Task<GradeAppealResponse> CreateAsync(Guid actorId, CreateGradeAppealRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated student account is required.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000) throw new ArgumentException("Provide an appeal reason of up to 1000 characters.");
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        if (!await _repository.GradeBelongsToStudentAsync(request.StudentCourseGradeId, studentId, ct))
            throw new KeyNotFoundException("Published course grade was not found for this student.");
        if (await _repository.HasPendingAppealAsync(request.StudentCourseGradeId, ct))
            throw new InvalidOperationException("A pending appeal already exists for this course grade.");
        var appeal = new GradeAppeal(request.StudentCourseGradeId, studentId, request.Reason, DateTimeOffset.UtcNow);
        await _repository.AddAppealAsync(appeal, ct);
        return (await _repository.GetStudentAppealsAsync(studentId, ct)).Single(x => x.Id == appeal.Id);
    }
    public async Task<IReadOnlyList<GradeAppealResponse>> GetMineAsync(Guid actorId, CancellationToken ct)
    {
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        return await _repository.GetStudentAppealsAsync(studentId, ct);
    }
    public async Task<IReadOnlyList<GradeAppealResponse>> GetPendingAsync(Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated reviewer is required.");
        return await _repository.GetPendingAppealsAsync(actorId, privileged, departmentAdmin, ct);
    }
    public async Task<IReadOnlyList<GradeAppealAuditResponse>> GetHistoryAsync(Guid appealId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        if (!await _repository.CanReviewAppealAsync(appealId, actorId, privileged, departmentAdmin, ct))
            throw new UnauthorizedAccessException("You cannot view this grade appeal history.");
        return await _repository.GetAppealHistoryAsync(appealId, ct);
    }
    public async Task<GradeAppealResponse> DecideAsync(Guid appealId, Guid actorId, bool privileged, bool departmentAdmin, DecideGradeAppealRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated reviewer is required.");
        if (string.IsNullOrWhiteSpace(request.Remarks) || request.Remarks.Trim().Length > 1000) throw new ArgumentException("Decision remarks are required.");
        if (!await _repository.CanReviewAppealAsync(appealId, actorId, privileged, departmentAdmin, ct))
            throw new UnauthorizedAccessException("You cannot review this grade appeal.");
        var appeal = await _repository.GetAppealAsync(appealId, ct) ?? throw new KeyNotFoundException("Grade appeal not found.");
        var previous = appeal.Status;
        var now = DateTimeOffset.UtcNow;
        appeal.Decide(request.Approve, actorId, request.Remarks, now);
        var audit = new GradeAppealAudit(appeal.Id, previous, appeal.Status, actorId, request.Remarks, now);
        await _repository.DecideAppealAsync(appeal, audit, ct);
        return (await _repository.GetStudentAppealsAsync(appeal.StudentId, ct)).Single(x => x.Id == appeal.Id);
    }
}
