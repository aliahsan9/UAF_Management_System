namespace UAF.Application.Common.Abstractions;

/// <summary>
/// Defines the application boundary for persisting a set of changes atomically.
/// The Infrastructure layer will provide the implementation.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
