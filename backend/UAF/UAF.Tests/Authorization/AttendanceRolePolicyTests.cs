using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class AttendanceRolePolicyTests
{
    [Theory]
    [InlineData("Teacher", true)]
    [InlineData("Registrar", true)]
    [InlineData("DepartmentAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Student", false)]
    [InlineData("FinanceOfficer", false)]
    [InlineData("HRManager", false)]
    public async Task Attendance_policy_allows_only_academic_staff_roles(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, null, AuthorizationPolicies.AttendanceManagement);
        Assert.Equal(expected, result.Succeeded);
    }
}
