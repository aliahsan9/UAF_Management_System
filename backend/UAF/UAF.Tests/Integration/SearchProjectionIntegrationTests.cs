using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UAF.Domain.Entities;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Integration;

public sealed class SearchProjectionIntegrationTests : IClassFixture<UafApiFactory>
{
    private const string SigningKey = "UAF-Integration-Test-Signing-Key-At-Least-32-Bytes";
    private readonly UafApiFactory _factory;

    public SearchProjectionIntegrationTests(UafApiFactory factory) => _factory = factory;

    [Fact]
    public async Task StudentSearch_RequiresAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/search/students?q=ali");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StudentSearch_ReturnsDirectoryFieldsWithoutIdentityLinkage()
    {
        var studentId = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
            db.Students.Add(new Student(
                "2026-CS-014",
                "Ali Hassan",
                "ali.hassan@uaf.edu.pk",
                Guid.NewGuid(),
                new DateOnly(2026, 9, 1),
                phoneNumber: "03001234567",
                identityUserId: Guid.NewGuid()));
            await db.SaveChangesAsync();
            studentId = await db.Students.Where(x => x.RegistrationNumber == "2026-CS-014").Select(x => x.Id).SingleAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("SuperAdmin"));
        var response = await client.GetAsync("/api/v1/search/students?q=Hassan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var match = document.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == studentId);
        Assert.Equal("Ali Hassan", match.GetProperty("fullName").GetString());
        Assert.Equal("2026-CS-014", match.GetProperty("registrationNumber").GetString());
        Assert.Equal("ali.hassan@uaf.edu.pk", match.GetProperty("email").GetString());
        Assert.False(match.TryGetProperty("identityUserId", out _));
        Assert.False(match.TryGetProperty("phoneNumber", out _));
        Assert.False(match.TryGetProperty("registeredByUserId", out _));
    }

    [Fact]
    public async Task WorkflowApproval_AcceptsDecisionCommentsInTheJsonBody()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Registrar"));
        var response = await client.PostAsJsonAsync($"/api/v1/workflows/{Guid.NewGuid()}/approve", new { comments = "Approved after review" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string Token(string role)
    {
        var userId = Guid.NewGuid();
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "UAF.Tests",
            audience: "UAF.Tests",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
