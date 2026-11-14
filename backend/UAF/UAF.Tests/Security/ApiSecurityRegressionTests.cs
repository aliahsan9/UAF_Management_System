using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace UAF.Tests.Integration;

public sealed class ApiSecurityRegressionTests : IClassFixture<UafApiFactory>
{
    private readonly HttpClient _client;

    public ApiSecurityRegressionTests(UafApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task AnonymousCaller_CannotReadStudentRecords()
    {
        var response = await _client.GetAsync("/api/v1/students");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginEndpoint_EnforcesIpBasedRateLimit()
    {
        using var isolatedFactory = new UafApiFactory();
        using var isolatedClient = isolatedFactory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var requestNumber = 0; requestNumber < 11; requestNumber++)
        {
            using var response = await isolatedClient.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = "", password = "" });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.BadRequest));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task RefreshEndpoint_RejectsRequestsWithoutCsrfHeaderAndCookie()
    {
        using var isolatedFactory = new UafApiFactory();
        using var isolatedClient = isolatedFactory.CreateClient();
        var response = await isolatedClient.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_DoesNotExposeServerInternals()
    {
        var response = await _client.GetAsync("/api/v1/not-a-real-endpoint");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidVerificationSignature_DoesNotReturnIdentityData()
    {
        var response = await _client.GetAsync(
            "/api/v1/verification/students/UAF-INVALID?signature=" + new string('0', 64));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
