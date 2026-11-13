using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading.Models;
public sealed record CreateGradeRevisionRequest(Guid AppealId, decimal ProposedMarksObtained, string Reason);
public sealed record DecideGradeRevisionRequest(bool Approve, string Remarks);
public sealed record GradeRevisionRequestResponse(Guid Id, Guid StudentCourseGradeId, Guid AppealId, Guid StudentId, string RegistrationNumber, string CourseCode, string CourseTitle, decimal CurrentMarks, decimal ProposedMarks, string Reason, GradeRevisionStatus Status, Guid RequestedByUserId, DateTimeOffset RequestedAtUtc, Guid? ReviewedByUserId, DateTimeOffset? ReviewedAtUtc, string? ReviewRemarks);
public sealed record GradeRevisionResponse(Guid Id, Guid StudentCourseGradeId, int RevisionNumber, decimal PreviousMarks, decimal NewMarks, decimal PreviousPercentage, decimal NewPercentage, string PreviousLetterGrade, string NewLetterGrade, decimal PreviousGradePoints, decimal NewGradePoints, bool PreviousIsPassed, bool NewIsPassed, string Reason, Guid RequestedByUserId, Guid ApprovedByUserId, DateTimeOffset ApprovedAtUtc);
