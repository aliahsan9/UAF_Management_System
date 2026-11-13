using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Grading;
using UAF.Application.Features.Grading.Models;
namespace UAF.API.Endpoints;
public static class GradeRevisionEndpoints
{
    public static IEndpointRouteBuilder MapGradeRevisionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var requests = endpoints.MapGroup("/api/v1/grading")
            .WithTags("Grade Revision Workflow")
            .RequireAuthorization(AuthorizationPolicies.GradeRevisionRequestManagement);
        requests.MapPost("/grades/{gradeId:guid}/revision-requests", async (Guid gradeId, CreateGradeRevisionRequest request,
            HttpContext http, GradeRevisionService service, CancellationToken ct) =>
        {
            var id = await service.RequestAsync(gradeId, GetActorId(http.User), IsPrivileged(http.User),
                http.User.IsInRole("DepartmentAdmin"), request, ct);
            return Results.Accepted($"/api/v1/grading/revision-requests/{id}", new { id, status = "PendingApproval" });
        }).WithName("RequestGradeRevision");
        requests.MapGet("/revision-requests/pending", async (HttpContext http, GradeRevisionService service, CancellationToken ct) =>
            Results.Ok(await service.GetPendingAsync(GetActorId(http.User), IsPrivileged(http.User), ct)))
            .WithName("GetPendingGradeRevisionRequests");

        var approvals = endpoints.MapGroup("/api/v1/grading/revision-requests")
            .WithTags("Grade Revision Approval")
            .RequireAuthorization(AuthorizationPolicies.GradeRevisionApproval);
        approvals.MapPost("/{requestId:guid}/decision", async (Guid requestId, DecideGradeRevisionRequest request,
            HttpContext http, GradeRevisionService service, CancellationToken ct) =>
            Results.Ok(await service.DecideAsync(requestId, GetActorId(http.User), request.Approve, request.Remarks, ct)))
            .WithName("DecideGradeRevisionRequest");

        var student = endpoints.MapGroup("/api/v1/academic-records/me/course-grades")
            .WithTags("Student Grade Revision History")
            .RequireAuthorization(AuthorizationPolicies.StudentAcademicRecord);
        student.MapGet("/{gradeId:guid}/revision-requests", async (Guid gradeId, HttpContext http, GradeRevisionService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyRequestsAsync(gradeId, GetActorId(http.User), ct)))
            .WithName("GetMyGradeRevisionRequests");
        student.MapGet("/{gradeId:guid}/revisions", async (Guid gradeId, HttpContext http, GradeRevisionService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyHistoryAsync(gradeId, GetActorId(http.User), ct)))
            .WithName("GetMyGradeRevisionHistory");
        return endpoints;
    }
    private static Guid GetActorId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id
            : throw new UnauthorizedAccessException("The authenticated account identifier is missing or invalid.");
    }
    private static bool IsPrivileged(ClaimsPrincipal principal) =>
        principal.IsInRole("Registrar") || principal.IsInRole("SuperAdmin");
}
