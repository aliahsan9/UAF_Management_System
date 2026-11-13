using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using UAF.API.Authorization;
using Xunit;

namespace UAF.Tests.Authorization;

public sealed class PermissionAuthorizationTests
{
    [Theory]
    [InlineData("FinanceOfficer", UafPermissions.FinanceManage, true)]
    [InlineData("FinanceOfficer", UafPermissions.AdmissionsDecide, false)]
    [InlineData("Student", UafPermissions.StudentsRead, true)]
    [InlineData("Student", UafPermissions.StudentsManage, false)]
    [InlineData("Registrar", UafPermissions.AdmissionsDecide, true)]
    [InlineData("Registrar", UafPermissions.FinanceManage, false)]
    [InlineData("SuperAdmin", UafPermissions.TranscriptsIssue, true)]
    public async Task PermissionPolicies_EnforceRolePermissionMatrix(
        string role, string permission, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
             new Claim(ClaimTypes.Role, role)], "test"));

        var result = await authorization.AuthorizeAsync(principal, null, $"Permission:{permission}");

        Assert.Equal(expected, result.Succeeded);
    }
}
