using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using UAF.API.Authorization;
using UAF.API.Endpoints;
using UAF.Application;
using Xunit;

namespace UAF.Tests.Authorization;

public sealed class AdmissionEndpointAuthorizationTests
{
    [Theory]
    [InlineData("/api/v1/admissions/{id:guid}/accept")]
    [InlineData("/api/v1/admissions/{id:guid}/reject")]
    [InlineData("/api/v1/admissions/{id:guid}/waitlist")]
    [InlineData("/api/v1/admissions/{id:guid}/decision-history")]
    [InlineData("/api/v1/admissions/registration-series")]
    [InlineData("/api/v1/admissions/{id:guid}/convert-to-student")]
    public async Task DecisionAndConversionEndpointsRequireDecisionPolicy(string route)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApplication();
        await using var app = builder.Build();
        app.MapAdmissionEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(x => x.RoutePattern.RawText == route);

        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(x => x.Policy).ToArray();

        Assert.Contains(AuthorizationPolicies.AdmissionsDecision, policies);
    }

    [Fact]
    public async Task ReviewStartRequiresAuthenticatedAdmissionsStaffPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApplication();
        await using var app = builder.Build();
        app.MapAdmissionEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(x => x.RoutePattern.RawText == "/api/v1/admissions/{id:guid}/start-review");

        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(x => x.Policy).ToArray();

        Assert.Contains(AuthorizationPolicies.AdmissionsStaff, policies);
    }
}
