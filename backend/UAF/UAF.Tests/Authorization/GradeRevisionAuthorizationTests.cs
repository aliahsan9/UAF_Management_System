using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class GradeRevisionAuthorizationTests
{
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("DepartmentAdmin", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task Only_registrar_or_superadmin_can_approve_grade_revisions(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, null, AuthorizationPolicies.GradeRevisionApproval);
        Assert.Equal(expected, result.Succeeded);
    }
}
