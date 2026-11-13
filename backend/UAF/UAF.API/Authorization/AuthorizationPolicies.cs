namespace UAF.API.Authorization;

/// <summary>
/// Centralized authorization policy names used by API endpoints.
/// Role names must match the roles provisioned by IdentityRoleSeeder.
/// </summary>
public static class AuthorizationPolicies
{
    public const string AdmissionsStaff = "Admissions.Staff";
    public const string AdmissionsDecision = "Admissions.Decision";
    public const string AcademicStructureAdmin = "AcademicStructure.Admin";
    public const string StudentManagement = "Students.Management";
}
