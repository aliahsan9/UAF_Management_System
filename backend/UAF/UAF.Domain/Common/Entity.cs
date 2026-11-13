namespace UAF.Domain.Common;

/// <summary>
/// Base type for domain entities with a strongly typed identity.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; private set; }
}
