using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class AttendancePolicyRoleTests
{
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("DepartmentAdmin", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    [InlineData("FinanceOfficer", false)]
    public async Task Only_registrar_and_super_admin_can_change_attendance_policy(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, null, AuthorizationPolicies.AttendancePolicyManagement);
        Assert.Equal(expected, result.Succeeded);
    }
}
