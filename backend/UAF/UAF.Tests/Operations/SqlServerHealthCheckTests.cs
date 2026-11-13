using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using UAF.API.Health;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Operations;

public sealed class SqlServerHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenSqlServerIsUnavailable()
    {
        var options = new DbContextOptionsBuilder<UafDbContext>()
            .UseSqlServer(
                "Server=127.0.0.1,1;Database=UafHealthCheckTests;User Id=invalid;Password=invalid;Connect Timeout=1;Encrypt=False;TrustServerCertificate=True")
            .Options;

        await using var dbContext = new UafDbContext(options);
        var healthCheck = new SqlServerHealthCheck(
            dbContext, NullLogger<SqlServerHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("SQL Server is unavailable.", result.Description);
        Assert.DoesNotContain("invalid", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_PropagatesCancellation()
    {
        var options = new DbContextOptionsBuilder<UafDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=UafHealthCheckTests;Connect Timeout=1;Encrypt=False")
            .Options;

        await using var dbContext = new UafDbContext(options);
        var healthCheck = new SqlServerHealthCheck(
            dbContext, NullLogger<SqlServerHealthCheck>.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellation.Token));
    }
}
