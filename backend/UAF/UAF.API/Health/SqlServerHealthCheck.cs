using Microsoft.Extensions.Diagnostics.HealthChecks;
using UAF.Infrastructure.Persistence;

namespace UAF.API.Health;

/// <summary>
/// Readiness check for the application's configured SQL Server database.
/// Does not expose connection strings or provider exception details to health endpoints.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly UafDbContext _dbContext;
    private readonly ILogger<SqlServerHealthCheck> _logger;

    public SqlServerHealthCheck(
        UafDbContext dbContext,
        ILogger<SqlServerHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "SQL Server readiness check failed.");
            return HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
    }
}
