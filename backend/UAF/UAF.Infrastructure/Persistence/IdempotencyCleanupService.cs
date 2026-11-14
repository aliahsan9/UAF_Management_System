using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UAF.Infrastructure.Persistence;

/// <summary>
/// Removes expired idempotency reservations. Academic, financial and audit records are
/// intentionally excluded from automated deletion.
/// </summary>
public sealed class IdempotencyCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyCleanupService> _logger;

    public IdempotencyCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<IdempotencyCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RemoveExpiredRecordsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Idempotency retention cleanup failed.");
            }
        }
    }

    private async Task RemoveExpiredRecordsAsync(CancellationToken cancellationToken)
    {
        var deleted = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
            var now = DateTimeOffset.UtcNow;
            var ids = await db.IdempotencyRecords
                .Where(record => record.ExpiresAtUtc <= now)
                .OrderBy(record => record.ExpiresAtUtc)
                .Select(record => record.Id)
                .Take(500)
                .ToArrayAsync(cancellationToken);

            if (ids.Length == 0) break;
            deleted += await db.IdempotencyRecords
                .Where(record => ids.Contains(record.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (deleted > 0)
            _logger.LogInformation("Expired idempotency records purged. DeletedCount: {DeletedCount}", deleted);
    }
}
