using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class ExaminationRolePolicyTests
{
    [Theory]
    [InlineData("Teacher", true)]
    [InlineData("Registrar", true)]
    [InlineData("DepartmentAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Student", false)]
    [InlineData("FinanceOfficer", false)]
    [InlineData("HRManager", false)]
    public async Task Only_academic_staff_can_manage_examinations(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, AuthorizationPolicies.ExaminationManagement);
        Assert.Equal(expected, result.Succeeded);
    }
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("DepartmentAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task Only_academic_administrators_can_approve_and_publish_results(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, AuthorizationPolicies.ExaminationApproval);
        Assert.Equal(expected, result.Succeeded);
    }
}
