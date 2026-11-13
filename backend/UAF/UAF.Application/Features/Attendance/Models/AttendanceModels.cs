using UAF.Domain.Enums;
namespace UAF.Application.Features.Attendance.Models;
public sealed record CreateAttendanceSessionRequest(Guid CourseOfferingId, DateOnly AttendanceDate, string? Topic);
public sealed record AttendanceMarkItem(Guid StudentId, AttendanceStatus Status, string? Reason);
public sealed record MarkAttendanceRequest(IReadOnlyCollection<AttendanceMarkItem> Marks);
public sealed record AttendanceMarkResponse(Guid StudentId, AttendanceStatus Status, string Remarks, Guid MarkedByUserId, DateTimeOffset MarkedAtUtc);
public sealed record AttendanceSessionResponse(Guid Id, Guid CourseOfferingId, DateOnly AttendanceDate, string Topic, AttendanceSessionStatus Status, Guid CreatedByUserId, Guid? FinalizedByUserId, DateTimeOffset? FinalizedAtUtc, IReadOnlyList<AttendanceMarkResponse> Marks);
