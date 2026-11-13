using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Append-only audit for grade revision approval and rejection decisions.</summary>
public sealed class GradeRevisionAudit : Entity
{
    private GradeRevisionAudit() { Remarks = string.Empty; }
    public GradeRevisionAudit(Guid requestId, GradeRevisionStatus previousStatus, GradeRevisionStatus newStatus, Guid actorId, string remarks, DateTimeOffset occurredAtUtc)
    {
        if (requestId == Guid.Empty || actorId == Guid.Empty || previousStatus != GradeRevisionStatus.PendingApproval ||
            newStatus is not GradeRevisionStatus.Approved and not GradeRevisionStatus.Rejected)
            throw new ArgumentException("A valid pending-to-final decision and actor are required.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length > 1000) throw new ArgumentException("Audit remarks are required.");
        if (occurredAtUtc == default) throw new ArgumentException("Audit timestamp is required.");
        RequestId = requestId; PreviousStatus = previousStatus; NewStatus = newStatus; ActorId = actorId;
        Remarks = remarks.Trim(); OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }
    public Guid RequestId { get; private set; }
    public GradeRevisionStatus PreviousStatus { get; private set; }
    public GradeRevisionStatus NewStatus { get; private set; }
    public Guid ActorId { get; private set; }
    public string Remarks { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
