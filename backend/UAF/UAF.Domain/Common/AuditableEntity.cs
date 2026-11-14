namespace UAF.Domain.Common;

/// <summary>
/// Base entity for records that require audit metadata, soft deletion and optimistic concurrency.
/// Historical academic records should be retained rather than physically removed.
/// </summary>
public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    /// <summary>SQL Server rowversion. The database generates this value on every update.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public void MarkUpdated(DateTimeOffset? updatedAtUtc = null) =>
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;

    public void SoftDelete(DateTimeOffset? deletedAtUtc = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc ?? DateTimeOffset.UtcNow;
        MarkUpdated(DeletedAtUtc);
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        MarkUpdated();
    }
}
