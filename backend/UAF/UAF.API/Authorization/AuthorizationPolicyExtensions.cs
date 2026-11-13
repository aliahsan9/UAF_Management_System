using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace UAF.API.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddUafAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var permission in UafPermissions.All)
                options.AddPolicy($"Permission:{permission}", policy =>
                    policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));

            options.AddPolicy(AuthorizationPolicies.AdmissionsStaff, policy =>
                policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.AdmissionsDecision, policy =>
                policy.RequireRole("Registrar", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.ApplicantSelfService, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Student"));
            options.AddPolicy(AuthorizationPolicies.AcademicStructureAdmin, policy =>
                policy.RequireRole("DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.StudentManagement, policy =>
                policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.AcademicCatalogManagement, policy =>
                policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.CourseOfferingManagement, policy =>
                policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.FinanceManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("FinanceOfficer", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.EmployeeManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("HRManager", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.AttendanceManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Teacher", "Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.AttendancePolicyManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.ExaminationManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Teacher", "Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.ExaminationApproval, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.GradingScaleManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.CourseGradeManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.StudentAcademicRecord, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Student"));
            options.AddPolicy(AuthorizationPolicies.GradeAppealReview, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.GradeRevisionRequestManagement, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.GradeRevisionApproval, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "SuperAdmin"));
            options.AddPolicy(AuthorizationPolicies.TranscriptIssuance, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Registrar", "SuperAdmin"));
        });
        return services;
    }
}
