using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private readonly ConditionalWeakTable<DbContext, Stopwatch> _timers = new();

    public UafSaveChangesAuditInterceptor(ILogger<UafSaveChangesAuditInterceptor> logger) =>
        _logger = logger;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartTimer(eventData.Context);
        LogPendingChanges(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        StartTimer(eventData.Context);
        LogPendingChanges(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        LogCommitted(eventData.Context, result);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        LogCommitted(eventData.Context, result);
        return result;
    }

    private void StartTimer(DbContext? context)
    {
        if (context is null) return;
        _timers.Remove(context);
        _timers.Add(context, Stopwatch.StartNew());
    }

    private void LogCommitted(DbContext? context, int affectedRows)
    {
        if (context is null) return;
        var elapsedMs = 0d;
        if (_timers.TryGetValue(context, out var stopwatch))
        {
            stopwatch.Stop();
            elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            _timers.Remove(context);
        }

        _logger.LogDebug(
            "Database unit of work committed. AffectedRows: {AffectedRows}, DurationMs: {DurationMs}",
            affectedRows, elapsedMs);
    }

    private void LogPendingChanges(DbContext? context)
    {
        if (context is null) return;
        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (entries.Length == 0) return;

        _logger.LogInformation(
            "Database unit of work started. Added: {AddedCount}, Modified: {ModifiedCount}, Deleted: {DeletedCount}",
            entries.Count(entry => entry.State == EntityState.Added),
            entries.Count(entry => entry.State == EntityState.Modified),
            entries.Count(entry => entry.State == EntityState.Deleted));
    }
}
