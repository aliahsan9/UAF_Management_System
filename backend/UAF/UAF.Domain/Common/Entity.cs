namespace UAF.Domain.Common;

/// <summary>
/// Root for persisted domain records. Shared concurrency and deletion metadata is mapped
/// once at the EF inheritance root so every domain record has consistent persistence semantics.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public virtual void SoftDelete(DateTimeOffset? deletedAtUtc = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public virtual void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
