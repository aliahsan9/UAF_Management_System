using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
    }
}
