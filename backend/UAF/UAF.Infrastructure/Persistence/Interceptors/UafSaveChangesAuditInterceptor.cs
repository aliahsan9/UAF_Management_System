using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UAF.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Records entity change counts for audit/operational observability. Business audit records
/// remain owned by the relevant application workflows and are not replaced by this telemetry.
/// </summary>
public sealed class UafSaveChangesAuditInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<UafSaveChangesAuditInterceptor> _logger;

    public UafSaveChangesAuditInterceptor(ILogger<UafSaveChangesAuditInterceptor> logger) =>
        _logger = logger;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogPendingChanges(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        LogPendingChanges(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Database unit of work committed. AffectedRows: {AffectedRows}, DurationMs: {DurationMs}",
            result, eventData.Duration.TotalMilliseconds);
        return ValueTask.FromResult(result);
    }

    private void LogPendingChanges(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        if (entries.Length == 0)
            return;

        _logger.LogInformation(
            "Database unit of work started. Added: {AddedCount}, Modified: {ModifiedCount}, Deleted: {DeletedCount}",
            entries.Count(entry => entry.State == EntityState.Added),
            entries.Count(entry => entry.State == EntityState.Modified),
            entries.Count(entry => entry.State == EntityState.Deleted));
    }
}