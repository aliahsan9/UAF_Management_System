namespace UAF.Domain.Common;

/// <summary>
/// Base entity that records when a domain record was created and last changed.
/// Soft-delete and concurrency metadata are inherited from Entity.
/// </summary>
public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public void MarkUpdated(DateTimeOffset? updatedAtUtc = null) =>
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;

    public override void SoftDelete(DateTimeOffset? deletedAtUtc = null)
    {
        base.SoftDelete(deletedAtUtc);
        MarkUpdated(DeletedAtUtc);
    }

    public override void Restore()
    {
        base.Restore();
        MarkUpdated();
    }
}
