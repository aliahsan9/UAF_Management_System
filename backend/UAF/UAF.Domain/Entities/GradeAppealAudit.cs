using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Append-only history of grade appeal decisions.</summary>
public sealed class GradeAppealAudit : Entity
{
    private GradeAppealAudit() { Remarks = string.Empty; }
    public GradeAppealAudit(Guid appealId, GradeAppealStatus previousStatus, GradeAppealStatus newStatus, Guid actorId, string remarks, DateTimeOffset occurredAtUtc)
    {
        if (appealId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Appeal and actor are required.");
        if (!Enum.IsDefined(previousStatus) || !Enum.IsDefined(newStatus) || previousStatus == newStatus) throw new ArgumentException("A valid status transition is required.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length > 1000) throw new ArgumentException("Audit remarks are required.");
        if (occurredAtUtc == default) throw new ArgumentException("Audit timestamp is required.");
        AppealId = appealId; PreviousStatus = previousStatus; NewStatus = newStatus; ActorId = actorId;
        Remarks = remarks.Trim(); OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }
    public Guid AppealId { get; private set; }
    public GradeAppealStatus PreviousStatus { get; private set; }
    public GradeAppealStatus NewStatus { get; private set; }
    public Guid ActorId { get; private set; }
    public string Remarks { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
