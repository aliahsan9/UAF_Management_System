using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace UAF.API.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddUafAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAuthorization(options =>
        {
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
        });
        return services;
    }
}
