using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using UAF.API.Authorization;
using Xunit;

namespace UAF.Tests.Authorization;

public sealed class AdmissionRolePolicyTests
{
    [Theory]
    [InlineData("Registrar", true, true)]
    [InlineData("SuperAdmin", true, true)]
    [InlineData("DepartmentAdmin", true, false)]
    [InlineData("Student", false, false)]
    public async Task AdmissionPoliciesEnforceStaffRoleBoundaries(
        string role, bool expectedStaffAccess, bool expectedDecisionAccess)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "test"));

        var staff = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.AdmissionsStaff);
        var decision = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.AdmissionsDecision);

        Assert.Equal(expectedStaffAccess, staff.Succeeded);
        Assert.Equal(expectedDecisionAccess, decision.Succeeded);
    }

    [Fact]
    public async Task ApplicantPolicyRequiresStudentRoleAndAuthenticatedIdentity()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await authorization.AuthorizeAsync(anonymous, null, AuthorizationPolicies.ApplicantSelfService);

        Assert.False(result.Succeeded);
    }
}
