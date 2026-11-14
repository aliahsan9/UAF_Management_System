using System.Net;using Microsoft.AspNetCore.Mvc.Testing;using Xunit;
namespace UAF.Tests.Integration;
public sealed class ApiPipelineIntegrationTests:IClassFixture<UafApiFactory>{
 private readonly HttpClient _client;
 public ApiPipelineIntegrationTests(UafApiFactory factory)=>_client=factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false});
 [Fact] public async Task LiveHealthEndpoint_IsAvailableWithoutDatabaseAccess(){var response=await _client.GetAsync("/health/live");Assert.Equal(HttpStatusCode.OK,response.StatusCode);}
 [Fact] public async Task ProtectedStudentEndpoint_RejectsAnonymousCaller(){var response=await _client.GetAsync("/api/v1/students");Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);}
 [Fact] public async Task SecurityHeaders_AreAppliedToResponses(){var response=await _client.GetAsync("/health/live");Assert.True(response.Headers.Contains("X-Content-Type-Options"));Assert.True(response.Headers.Contains("X-Frame-Options"));}
 [Fact] public async Task OpenApiContract_IsPublishedInDevelopment(){var response=await _client.GetAsync("/openapi/v1.json");Assert.Equal(HttpStatusCode.OK,response.StatusCode);var json=await response.Content.ReadAsStringAsync();Assert.Contains("/api/v1/students",json,StringComparison.Ordinal);}
}
