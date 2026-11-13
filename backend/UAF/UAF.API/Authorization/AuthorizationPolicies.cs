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
    public const string AttendanceManagement = "Attendance.Management";
    public const string AttendancePolicyManagement = "Attendance.PolicyManagement";
    public const string ExaminationManagement = "Examinations.Management";
    public const string ExaminationApproval = "Examinations.Approval";
    public const string GradingScaleManagement = "Grading.ScaleManagement";
    public const string CourseGradeManagement = "Grading.CourseGradeManagement";
    public const string StudentAcademicRecord = "AcademicRecords.Student";
    public const string GradeAppealReview = "Grading.GradeAppealReview";
    public const string GradeRevisionRequestManagement = "Grading.GradeRevisionRequestManagement";
    public const string GradeRevisionApproval = "Grading.GradeRevisionApproval";
}
