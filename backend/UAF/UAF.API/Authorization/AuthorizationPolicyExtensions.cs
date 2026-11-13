using Microsoft.AspNetCore.Authorization;
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
                AddPermissionPolicy(options, $"Permission:{permission}", permission);

            AddPermissionPolicy(options, AuthorizationPolicies.AdmissionsStaff, UafPermissions.AdmissionsReview);
            AddPermissionPolicy(options, AuthorizationPolicies.AdmissionsDecision, UafPermissions.AdmissionsDecide);
            options.AddPolicy(AuthorizationPolicies.ApplicantSelfService, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Student"));
            AddPermissionPolicy(options, AuthorizationPolicies.AcademicStructureAdmin, UafPermissions.AcademicStructureManage);
            AddPermissionPolicy(options, AuthorizationPolicies.StudentManagement, UafPermissions.StudentsManage);
            AddPermissionPolicy(options, AuthorizationPolicies.AcademicCatalogManagement, UafPermissions.AcademicCatalogManage);
            AddPermissionPolicy(options, AuthorizationPolicies.CourseOfferingManagement, UafPermissions.CourseOfferingsManage);
            AddPermissionPolicy(options, AuthorizationPolicies.FinanceManagement, UafPermissions.FinanceManage);
            AddPermissionPolicy(options, AuthorizationPolicies.EmployeeManagement, UafPermissions.EmployeesManage);
            AddPermissionPolicy(options, AuthorizationPolicies.AttendanceManagement, UafPermissions.AttendanceManage);
            AddPermissionPolicy(options, AuthorizationPolicies.AttendancePolicyManagement, UafPermissions.AttendancePolicyManage);
            AddPermissionPolicy(options, AuthorizationPolicies.ExaminationManagement, UafPermissions.ExaminationsManage);
            AddPermissionPolicy(options, AuthorizationPolicies.ExaminationApproval, UafPermissions.ExaminationsApprove);
            AddPermissionPolicy(options, AuthorizationPolicies.GradingScaleManagement, UafPermissions.GradingScalesManage);
            AddPermissionPolicy(options, AuthorizationPolicies.CourseGradeManagement, UafPermissions.CourseGradesManage);
            options.AddPolicy(AuthorizationPolicies.StudentAcademicRecord, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Student"));
            AddPermissionPolicy(options, AuthorizationPolicies.GradeAppealReview, UafPermissions.GradeAppealsReview);
            AddPermissionPolicy(options, AuthorizationPolicies.GradeRevisionRequestManagement, UafPermissions.GradeRevisionsManage);
            AddPermissionPolicy(options, AuthorizationPolicies.GradeRevisionApproval, UafPermissions.GradeRevisionsApprove);
            AddPermissionPolicy(options, AuthorizationPolicies.TranscriptIssuance, UafPermissions.TranscriptsIssue);
        });
        return services;
    }

    private static void AddPermissionPolicy(
        AuthorizationOptions options, string policyName, string permission)
    {
        options.AddPolicy(policyName, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
    }
}
