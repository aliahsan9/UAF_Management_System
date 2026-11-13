using UAF.Domain.Enums;
namespace UAF.Application.Features.Examinations.Models;
public sealed record CreateExaminationRequest(Guid CourseOfferingId, string Title, ExaminationType Type, DateOnly ScheduledDate, decimal MaximumMarks, decimal PassingMarks, decimal WeightPercentage);
public sealed record ExaminationMarkItem(Guid StudentId, decimal MarksObtained, bool IsAbsent, string? Remarks, string Reason);
public sealed record SaveExaminationMarksRequest(IReadOnlyList<ExaminationMarkItem> Marks);
public sealed record ExaminationResponse(Guid Id, Guid CourseOfferingId, string Title, ExaminationType Type, DateOnly ScheduledDate, decimal MaximumMarks, decimal PassingMarks, ExaminationStatus Status, Guid? SubmittedByUserId, DateTimeOffset? SubmittedAtUtc, Guid? ApprovedByUserId, DateTimeOffset? ApprovedAtUtc, Guid? PublishedByUserId, DateTimeOffset? PublishedAtUtc);
public sealed record ExaminationResultView(Guid StudentId, string RegistrationNumber, string FullName, decimal MarksObtained, bool IsAbsent, bool IsPassed, string Remarks);
public sealed record ExaminationListItem(Guid Id, string Title, ExaminationType Type, DateOnly ScheduledDate, decimal MaximumMarks, decimal PassingMarks, decimal WeightPercentage, ExaminationStatus Status);
