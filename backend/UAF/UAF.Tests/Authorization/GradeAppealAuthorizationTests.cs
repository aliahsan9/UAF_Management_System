using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class GradeAppealAuthorizationTests
{
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("DepartmentAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    [InlineData("FinanceOfficer", false)]
    public async Task Only_academic_staff_can_review_grade_appeals(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(user, null, AuthorizationPolicies.GradeAppealReview);
        Assert.Equal(expected, result.Succeeded);
    }
}
