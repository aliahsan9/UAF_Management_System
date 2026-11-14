using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UAF.Infrastructure.Persistence;

namespace UAF.Tests.Integration;

public sealed class UafApiFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:UafDatabase",
            "Server=127.0.0.1,1433;Database=master;User Id=sa;Password=Uaf_CI_StrongPassword_2026!;Encrypt=False;TrustServerCertificate=True");
        builder.UseSetting("Jwt:Issuer", "UAF.Tests");
        builder.UseSetting("Jwt:Audience", "UAF.Tests");
        builder.UseSetting("Jwt:SigningKey", "UAF-Integration-Test-Signing-Key-At-Least-32-Bytes");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "14");
        builder.UseSetting("AdmissionDocumentStorage:RootPath",
            Path.Combine(Path.GetTempPath(), "uaf-api-tests", Guid.NewGuid().ToString("N")));
        builder.UseSetting("Cors:AllowedOrigins:0", "https://localhost:4200");

        builder.ConfigureServices(services =>
        {
            var databaseName = $"UafApiPipeline_{Guid.NewGuid():N}";
            services.RemoveAll<IDbContextOptionsConfiguration<UafDbContext>>();
            services.RemoveAll<DbContextOptions<UafDbContext>>();
            services.RemoveAll<UafDbContext>();
            services.AddDbContext<UafDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
