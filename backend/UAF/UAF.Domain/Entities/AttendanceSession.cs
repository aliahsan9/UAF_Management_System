using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>A single attendance-taking event for one scheduled course offering.</summary>
public sealed class AttendanceSession : AuditableEntity
{
    private AttendanceSession() { Topic = string.Empty; }
    public AttendanceSession(Guid courseOfferingId, DateOnly attendanceDate, Guid createdByUserId, string? topic = null)
    {
        if (courseOfferingId == Guid.Empty) throw new ArgumentException("Course offering is required.", nameof(courseOfferingId));
        if (attendanceDate == default) throw new ArgumentOutOfRangeException(nameof(attendanceDate));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(createdByUserId));
        if (topic?.Trim().Length > 200) throw new ArgumentOutOfRangeException(nameof(topic));
        CourseOfferingId = courseOfferingId;
        AttendanceDate = attendanceDate;
        CreatedByUserId = createdByUserId;
        Topic = topic?.Trim() ?? string.Empty;
        Status = AttendanceSessionStatus.Open;
    }
    public Guid CourseOfferingId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Topic { get; private set; }
    public AttendanceSessionStatus Status { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public void Finalize(Guid actorUserId, DateTimeOffset now)
    {
        if (Status != AttendanceSessionStatus.Open) throw new InvalidOperationException("Only an open attendance session can be finalized.");
        if (actorUserId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorUserId));
        if (now == default) throw new ArgumentException("Finalization timestamp is required.", nameof(now));
        Status = AttendanceSessionStatus.Finalized;
        FinalizedByUserId = actorUserId;
        FinalizedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    public void Cancel()
    {
        if (Status != AttendanceSessionStatus.Open) throw new InvalidOperationException("Only an open attendance session can be cancelled.");
        Status = AttendanceSessionStatus.Cancelled;
        MarkUpdated();
    }
}
