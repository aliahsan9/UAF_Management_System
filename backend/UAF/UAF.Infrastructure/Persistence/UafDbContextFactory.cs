using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UAF.Infrastructure.Persistence;

/// <summary>
/// Enables EF Core tooling to create the context for migrations without requiring the API host.
/// Configure UAF_DB_CONNECTION_STRING to use a non-default database.
/// </summary>
public sealed class UafDbContextFactory : IDesignTimeDbContextFactory<UafDbContext>
{
    public UafDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("UAF_DB_CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=UafManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<UafDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new UafDbContext(options);
    }
}
