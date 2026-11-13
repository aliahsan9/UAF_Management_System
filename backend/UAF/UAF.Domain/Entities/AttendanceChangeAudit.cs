using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Append-only history for every attendance mark and subsequent correction.</summary>
public sealed class AttendanceChangeAudit : Entity
{
    private AttendanceChangeAudit() { Reason = string.Empty; }
    public AttendanceChangeAudit(Guid sessionId, Guid studentId, Guid actorUserId, AttendanceStatus? previousStatus, AttendanceStatus newStatus, string? reason, DateTimeOffset changedAtUtc)
    {
        if (sessionId == Guid.Empty || studentId == Guid.Empty || actorUserId == Guid.Empty) throw new ArgumentException("Session, student and actor are required.");
        if (!Enum.IsDefined(newStatus) || (previousStatus.HasValue && !Enum.IsDefined(previousStatus.Value))) throw new ArgumentOutOfRangeException(nameof(newStatus));
        if (reason?.Trim().Length > 500) throw new ArgumentOutOfRangeException(nameof(reason));
        if (changedAtUtc == default) throw new ArgumentException("Audit timestamp is required.");
        AttendanceSessionId = sessionId; StudentId = studentId; ActorUserId = actorUserId;
        PreviousStatus = previousStatus; NewStatus = newStatus; Reason = reason?.Trim() ?? string.Empty; ChangedAtUtc = changedAtUtc.ToUniversalTime();
    }
    public Guid AttendanceSessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public AttendanceStatus? PreviousStatus { get; private set; }
    public AttendanceStatus NewStatus { get; private set; }
    public string Reason { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
}
