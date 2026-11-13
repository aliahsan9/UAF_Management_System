using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading;
public sealed class GradeRevisionService
{
    private readonly IGradeRevisionRepository _repository;
    public GradeRevisionService(IGradeRevisionRepository repository) => _repository = repository;

    public async Task<Guid> RequestAsync(Guid gradeId, Guid actorId, bool privileged, bool departmentAdmin, CreateGradeRevisionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (gradeId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Course grade and authenticated actor are required.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000) throw new ArgumentException("A revision reason of up to 1000 characters is required.");
        if (!await _repository.CanRequestRevisionAsync(gradeId, actorId, privileged, departmentAdmin, ct))
            throw new UnauthorizedAccessException("You cannot request a grade revision for this course.");
        var context = await _repository.GetRevisionContextAsync(gradeId, ct) ?? throw new KeyNotFoundException("Finalized course grade not found.");
        if (!context.Scale.IsPublished) throw new InvalidOperationException("The original published grading scale is unavailable.");
        if (request.ProposedMarksObtained < 0 || request.ProposedMarksObtained > context.Grade.TotalMarks)
            throw new ArgumentOutOfRangeException(nameof(request.ProposedMarksObtained), "Proposed marks must be within the original total marks.");
        if (!await _repository.ApprovedAppealExistsAsync(request.AppealId, gradeId, ct))
            throw new InvalidOperationException("A grade revision requires an approved appeal for this exact course grade.");
        if (await _repository.HasPendingRequestAsync(gradeId, ct))
            throw new InvalidOperationException("A pending grade revision request already exists for this course grade.");
        var entity = new GradeRevisionRequest(gradeId, request.AppealId, request.ProposedMarksObtained, request.Reason, actorId, DateTimeOffset.UtcNow);
        await _repository.AddRequestAsync(entity, ct);
        return entity.Id;
    }

    public Task<IReadOnlyList<GradeRevisionRequestResponse>> GetPendingAsync(Guid actorId, bool privileged, CancellationToken ct)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated reviewer is required.");
        return _repository.GetPendingRequestsAsync(actorId, privileged, ct);
    }

    public async Task<GradeRevisionDecisionResponse> DecideAsync(Guid requestId, Guid actorId, bool approve, string remarks, CancellationToken ct)
    {
        if (requestId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Revision request and reviewer are required.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length > 1000) throw new ArgumentException("Review remarks are required (maximum 1000 characters).");
        var request = await _repository.GetRequestAsync(requestId, ct) ?? throw new KeyNotFoundException("Grade revision request not found.");
        if (request.Status != GradeRevisionStatus.PendingApproval) throw new InvalidOperationException("This grade revision request has already been decided.");
        if (request.RequestedByUserId == actorId) throw new UnauthorizedAccessException("The requesting officer cannot approve or reject their own grade revision.");
        GradeRevision? revision = null;
        var expectedRevisionNumber = 0;
        if (approve)
        {
            var context = await _repository.GetRevisionContextAsync(request.StudentCourseGradeId, ct)
                ?? throw new KeyNotFoundException("Finalized course grade not found.");
            if (!context.Scale.IsPublished) throw new InvalidOperationException("The original grading scale must remain published.");
            var grade = context.Grade;
            if (request.ProposedMarksObtained > grade.TotalMarks) throw new InvalidOperationException("Proposed marks exceed the original total marks.");
            var percentage = decimal.Round(request.ProposedMarksObtained / grade.TotalMarks * 100m, 2, MidpointRounding.AwayFromZero);
            var band = context.Scale.Bands.Where(x => x.MinimumPercentage <= percentage)
                .OrderByDescending(x => x.MinimumPercentage).FirstOrDefault()
                ?? throw new InvalidOperationException("The published grading scale does not cover the revised percentage.");
            var latest = context.LatestRevision;
            expectedRevisionNumber = latest?.RevisionNumber ?? 0;
            revision = new GradeRevision(request.Id, grade.Id, expectedRevisionNumber + 1,
                latest?.NewMarks ?? grade.MarksObtained, request.ProposedMarksObtained,
                latest?.NewPercentage ?? grade.Percentage, percentage,
                latest?.NewLetterGrade ?? grade.LetterGrade, band.LetterGrade,
                latest?.NewGradePoints ?? grade.GradePoints, band.GradePoints,
                latest?.NewIsPassed ?? grade.IsPassed, band.IsPassing,
                request.Reason, request.RequestedByUserId, actorId, DateTimeOffset.UtcNow);
        }
        var now = DateTimeOffset.UtcNow;
        var nextStatus = approve ? GradeRevisionStatus.Approved : GradeRevisionStatus.Rejected;
        var audit = new GradeRevisionAudit(request.Id, GradeRevisionStatus.PendingApproval, nextStatus, actorId, remarks, now);
        await _repository.DecideRequestAsync(request.Id, actorId, approve, remarks, revision, audit, expectedRevisionNumber, ct);
        return new GradeRevisionDecisionResponse(request.Id, nextStatus, revision is null ? null : ToResponse(revision));
    }

    public async Task<IReadOnlyList<GradeRevisionRequestResponse>> GetMyRequestsAsync(Guid gradeId, Guid actorId, CancellationToken ct)
    {
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        if (!await _repository.StudentOwnsGradeAsync(gradeId, studentId, ct))
            throw new KeyNotFoundException("Course grade not found for this student.");
        return await _repository.GetStudentRequestsAsync(gradeId, studentId, ct);
    }

    public async Task<IReadOnlyList<GradeRevisionResponse>> GetMyHistoryAsync(Guid gradeId, Guid actorId, CancellationToken ct)
    {
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        if (!await _repository.StudentOwnsGradeAsync(gradeId, studentId, ct))
            throw new KeyNotFoundException("Course grade not found for this student.");
        return await _repository.GetRevisionHistoryAsync(gradeId, ct);
    }
    private static GradeRevisionResponse ToResponse(GradeRevision x) =>
        new(x.Id, x.StudentCourseGradeId, x.RevisionNumber, x.PreviousMarks, x.NewMarks, x.PreviousPercentage,
            x.NewPercentage, x.PreviousLetterGrade, x.NewLetterGrade, x.PreviousGradePoints, x.NewGradePoints,
            x.PreviousIsPassed, x.NewIsPassed, x.Reason, x.RequestedByUserId, x.ApprovedByUserId, x.ApprovedAtUtc);
}
