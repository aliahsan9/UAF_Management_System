namespace UAF.API.Authorization;

public static class AuthorizationPolicies
{
    public const string AdmissionsStaff = "Admissions.Staff";
    public const string AdmissionsDecision = "Admissions.Decision";
    public const string ApplicantSelfService = "Admissions.Applicant";
    public const string AcademicStructureAdmin = "AcademicStructure.Admin";
    public const string StudentManagement = "Students.Management";
    public const string AcademicCatalogManagement = "AcademicCatalog.Management";
    public const string CourseOfferingManagement = "CourseOfferings.Management";
    public const string FinanceManagement = "Finance.Management";
    public const string EmployeeManagement = "Employees.Management";
}
