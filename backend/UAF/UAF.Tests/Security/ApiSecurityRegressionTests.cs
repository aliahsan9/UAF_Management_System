using System.Net;using Xunit;
namespace UAF.Tests.Integration;
public sealed class ApiSecurityRegressionTests:IClassFixture<UafApiFactory>{
 private readonly HttpClient _client;public ApiSecurityRegressionTests(UafApiFactory factory)=>_client=factory.CreateClient();
 [Fact] public async Task AnonymousCaller_CannotReadStudentRecords(){var response=await _client.GetAsync("/api/v1/students");Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);}
 [Fact] public async Task UnknownRoute_DoesNotExposeServerInternals(){var response=await _client.GetAsync("/api/v1/not-a-real-endpoint");Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);var body=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("ConnectionString",body,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("StackTrace",body,StringComparison.OrdinalIgnoreCase);}
 [Fact] public async Task InvalidVerificationSignature_DoesNotReturnIdentityData(){var response=await _client.GetAsync("/api/v1/verification/students/UAF-INVALID?signature="+new string('0',64));Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);}
}
