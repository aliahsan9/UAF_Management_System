using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Current attendance state for one student in one attendance session.</summary>
public sealed class StudentAttendance : AuditableEntity
{
    private StudentAttendance() { Remarks = string.Empty; }
    public StudentAttendance(Guid sessionId, Guid studentId, AttendanceStatus status, Guid markedByUserId, DateTimeOffset markedAtUtc, string? remarks = null)
    {
        if (sessionId == Guid.Empty || studentId == Guid.Empty || markedByUserId == Guid.Empty) throw new ArgumentException("Session, student and actor are required.");
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (markedAtUtc == default) throw new ArgumentException("Marking timestamp is required.");
        Remarks = NormalizeRemarks(remarks);
        AttendanceSessionId = sessionId; StudentId = studentId; Status = status;
        MarkedByUserId = markedByUserId; MarkedAtUtc = markedAtUtc.ToUniversalTime();
    }
    public Guid AttendanceSessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string Remarks { get; private set; }
    public Guid MarkedByUserId { get; private set; }
    public DateTimeOffset MarkedAtUtc { get; private set; }
    public void Correct(AttendanceStatus status, Guid actorUserId, DateTimeOffset now, string reason)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (actorUserId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorUserId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required when changing an existing attendance mark.", nameof(reason));
        if (reason.Trim().Length > 500) throw new ArgumentOutOfRangeException(nameof(reason));
        Status = status; Remarks = NormalizeRemarks(reason); MarkedByUserId = actorUserId; MarkedAtUtc = now.ToUniversalTime(); MarkUpdated(now);
    }
    private static string NormalizeRemarks(string? value)
    {
        if (value?.Trim().Length > 500) throw new ArgumentOutOfRangeException(nameof(value));
        return value?.Trim() ?? string.Empty;
    }
}
