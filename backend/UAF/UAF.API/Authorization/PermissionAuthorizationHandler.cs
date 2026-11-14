using Microsoft.AspNetCore.Authorization;
namespace UAF.API.Authorization;
/// <summary>Maps trusted Identity roles to explicit application permissions.</summary>
public sealed class PermissionAuthorizationHandler:AuthorizationHandler<PermissionRequirement>{
 private static readonly IReadOnlyDictionary<string,string[]> RolePermissions=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase){
 ["Student"]=[UafPermissions.AdmissionsRead,UafPermissions.StudentsRead],
 ["Teacher"]=[UafPermissions.StudentsRead,UafPermissions.AttendanceManage,UafPermissions.ExaminationsManage,UafPermissions.GradesManage],
 ["Registrar"]=UafPermissions.All.Where(p=>p is not UafPermissions.FinanceRead and not UafPermissions.FinanceManage and not UafPermissions.EmployeesManage and not UafPermissions.AcademicStructureManage and not UafPermissions.AuditTrailRead).ToArray(),
 ["DepartmentAdmin"]=[UafPermissions.AdmissionsRead,UafPermissions.AdmissionsReview,UafPermissions.StudentsRead,UafPermissions.StudentsManage,UafPermissions.AcademicStructureManage,UafPermissions.AcademicCatalogManage,UafPermissions.CourseOfferingsManage,UafPermissions.CourseGradesManage,UafPermissions.AttendanceManage,UafPermissions.ExaminationsManage,UafPermissions.ExaminationsApprove,UafPermissions.GradesManage,UafPermissions.GradeAppealsReview,UafPermissions.GradeRevisionsManage,UafPermissions.TimetableManage,UafPermissions.WorkflowsManage,UafPermissions.AnalyticsRead],
 ["FinanceOfficer"]=[UafPermissions.FinanceRead,UafPermissions.FinanceManage,UafPermissions.AnalyticsRead],
 ["HRManager"]=[UafPermissions.EmployeesManage],
 ["SuperAdmin"]=UafPermissions.All};
 protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,PermissionRequirement requirement){if(context.User.Identity?.IsAuthenticated!=true)return Task.CompletedTask;var granted=context.User.FindAll(System.Security.Claims.ClaimTypes.Role).SelectMany(r=>RolePermissions.TryGetValue(r.Value,out var p)?p:Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);if(granted.Contains(requirement.Permission))context.Succeed(requirement);return Task.CompletedTask;}
}
