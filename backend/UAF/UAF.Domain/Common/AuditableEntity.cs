namespace UAF.Domain.Common;

/// <summary>
/// Base entity that records when a domain record was created and last changed.
/// </summary>
public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public void MarkUpdated(DateTimeOffset? updatedAtUtc = null)
    {
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
    }
}
