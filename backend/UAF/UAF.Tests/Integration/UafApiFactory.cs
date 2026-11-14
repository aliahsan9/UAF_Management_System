using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UAF.Tests.Integration;

public sealed class UafApiFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UafDatabase"] = "Server=127.0.0.1,1433;Database=UafApiFactory_NotUsed;User Id=sa;Password=Uaf_CI_StrongPassword_2026!;Encrypt=False;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "UAF.Tests",
                ["Jwt:Audience"] = "UAF.Tests",
                ["Jwt:SigningKey"] = "UAF-Integration-Test-Signing-Key-At-Least-32-Bytes",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["AdmissionDocumentStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "uaf-api-tests", Guid.NewGuid().ToString("N")),
                ["Cors:AllowedOrigins:0"] = "https://localhost:4200"
            }));
    }
}
