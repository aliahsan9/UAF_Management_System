using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading.Models;
public sealed record CreateGradeAppealRequest(Guid StudentCourseGradeId, string Reason);
public sealed record DecideGradeAppealRequest(bool Approve, string Remarks);
public sealed record GradeAppealResponse(Guid Id, Guid StudentCourseGradeId, Guid StudentId, string RegistrationNumber, string CourseCode, string CourseTitle, string AcademicSessionName, string Reason, GradeAppealStatus Status, DateTimeOffset SubmittedAtUtc, Guid? ReviewedByUserId, DateTimeOffset? ReviewedAtUtc, string? ReviewRemarks);
