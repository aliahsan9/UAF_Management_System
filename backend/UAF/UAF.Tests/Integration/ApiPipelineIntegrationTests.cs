using System.Net;
using System.Text.Json;
using Xunit;

namespace UAF.Tests.Integration;

public sealed class ApiPipelineIntegrationTests : IClassFixture<UafApiFactory>
{
    private readonly HttpClient _client;

    public ApiPipelineIntegrationTests(UafApiFactory factory) =>
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task LiveHealthEndpoint_IsAvailableWithoutDatabaseAccess()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedStudentEndpoint_RejectsAnonymousCaller()
    {
        var response = await _client.GetAsync("/api/v1/students");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_AreAppliedToResponses()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task OpenApiContract_ContainsVersionedStudentAndAuthenticationOperations()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/students", out var students));
        Assert.True(students.TryGetProperty("get", out _));
        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var login));
        Assert.True(login.TryGetProperty("post", out _));
    }
}
