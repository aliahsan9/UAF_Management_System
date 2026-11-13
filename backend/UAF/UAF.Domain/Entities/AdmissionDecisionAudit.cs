using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// Append-only record of a staff-controlled admission workflow transition.
/// </summary>
public sealed class AdmissionDecisionAudit : Entity
{
    private AdmissionDecisionAudit() { }

    public AdmissionDecisionAudit(
        Guid applicationId, Guid actorUserId, AdmissionStatus fromStatus,
        AdmissionStatus toStatus, string? remarks, DateTimeOffset occurredAtUtc)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("Application is required.", nameof(applicationId));
        if (actorUserId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorUserId));
        if (!Enum.IsDefined(fromStatus) || !Enum.IsDefined(toStatus))
            throw new ArgumentOutOfRangeException(nameof(toStatus));
        if (fromStatus == toStatus) throw new ArgumentException("An audit transition must change status.");
        if (occurredAtUtc == default) throw new ArgumentException("A UTC timestamp is required.", nameof(occurredAtUtc));
        var normalizedRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        if (normalizedRemarks?.Length > 2000) throw new ArgumentException("Remarks cannot exceed 2000 characters.", nameof(remarks));

        ApplicationId = applicationId;
        ActorUserId = actorUserId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Remarks = normalizedRemarks;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public Guid ApplicationId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public AdmissionStatus FromStatus { get; private set; }
    public AdmissionStatus ToStatus { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
