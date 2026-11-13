using Microsoft.AspNetCore.Authorization;

namespace UAF.API.Authorization;

/// <summary>Maps trusted Identity roles to explicit application permissions.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private static readonly IReadOnlyDictionary<string, string[]> RolePermissions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Student"] = [UafPermissions.AdmissionsRead, UafPermissions.StudentsRead],
            ["Teacher"] = [UafPermissions.StudentsRead, UafPermissions.AttendanceManage, UafPermissions.GradesManage],
            ["Registrar"] = UafPermissions.All,
            ["DepartmentAdmin"] = [UafPermissions.AdmissionsRead, UafPermissions.AdmissionsReview,
                UafPermissions.StudentsRead, UafPermissions.StudentsManage, UafPermissions.AttendanceManage, UafPermissions.GradesManage],
            ["FinanceOfficer"] = [UafPermissions.FinanceRead, UafPermissions.FinanceManage],
            ["HRManager"] = [UafPermissions.EmployeesManage],
            ["SuperAdmin"] = UafPermissions.All
        };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var granted = context.User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .SelectMany(role => RolePermissions.TryGetValue(role.Value, out var permissions)
                ? permissions : Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (granted.Contains(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
