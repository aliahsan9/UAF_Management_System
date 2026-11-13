namespace UAF.API.Authorization;

public static class UafPermissions
{
    public const string AdmissionsRead = "admissions.read";
    public const string AdmissionsReview = "admissions.review";
    public const string AdmissionsDecide = "admissions.decide";
    public const string StudentsRead = "students.read";
    public const string StudentsManage = "students.manage";
    public const string FinanceRead = "finance.read";
    public const string FinanceManage = "finance.manage";
    public const string EmployeesManage = "employees.manage";
    public const string AttendanceManage = "attendance.manage";
    public const string GradesManage = "grades.manage";
    public const string TranscriptsIssue = "transcripts.issue";

    public static readonly string[] All =
    [
        AdmissionsRead, AdmissionsReview, AdmissionsDecide, StudentsRead, StudentsManage,
        FinanceRead, FinanceManage, EmployeesManage, AttendanceManage, GradesManage, TranscriptsIssue
    ];
}
