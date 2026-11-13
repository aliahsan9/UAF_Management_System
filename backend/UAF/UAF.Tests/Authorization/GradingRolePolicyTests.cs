using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class GradingRolePolicyTests
{
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("DepartmentAdmin", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task Only_registrar_or_superadmin_can_manage_grade_scales(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, null, AuthorizationPolicies.GradingScaleManagement);
        Assert.Equal(expected, result.Succeeded);
    }
    [Theory]
    [InlineData("Registrar", true)]
    [InlineData("DepartmentAdmin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    [InlineData("FinanceOfficer", false)]
    public async Task Only_academic_administrators_can_finalize_course_grades(string role, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, null, AuthorizationPolicies.CourseGradeManagement);
        Assert.Equal(expected, result.Succeeded);
    }
    [Fact]
    public async Task Only_students_can_access_student_self_service_academic_records()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUafAuthorization();
        using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthorizationService>();
        var student = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Student") }, "test"));
        var registrar = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Registrar") }, "test"));
        Assert.True((await auth.AuthorizeAsync(student, null, AuthorizationPolicies.StudentAcademicRecord)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(registrar, null, AuthorizationPolicies.StudentAcademicRecord)).Succeeded);
    }
}
