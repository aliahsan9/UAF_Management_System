using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Attendance;
using UAF.Application.Features.Attendance.Models;
namespace UAF.API.Endpoints;
public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/attendance")
            .WithTags("Attendance")
            .RequireAuthorization(AuthorizationPolicies.AttendanceManagement);

        group.MapPost("/sessions", async (CreateAttendanceSessionRequest request, HttpContext http, AttendanceService service, CancellationToken ct) =>
        {
            var actor = GetActorId(http.User);
            var id = await service.CreateSessionAsync(request, actor, IsPrivileged(http.User), ct);
            return Results.Created($"/api/v1/attendance/sessions/{id}", new { id });
        }).WithName("CreateAttendanceSession");

        group.MapGet("/sessions/{id:guid}", async (Guid id, HttpContext http, AttendanceService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, GetActorId(http.User), IsPrivileged(http.User), ct)))
            .WithName("GetAttendanceSession");

        group.MapPut("/sessions/{id:guid}/marks", async (Guid id, MarkAttendanceRequest request, HttpContext http, AttendanceService service, CancellationToken ct) =>
        {
            await service.MarkAsync(id, GetActorId(http.User), IsPrivileged(http.User), request, ct);
            return Results.NoContent();
        }).WithName("MarkStudentAttendance");

        group.MapPost("/sessions/{id:guid}/finalize", async (Guid id, HttpContext http, AttendanceService service, CancellationToken ct) =>
        {
            await service.FinalizeAsync(id, GetActorId(http.User), IsPrivileged(http.User), ct);
            return Results.NoContent();
        }).WithName("FinalizeAttendanceSession");

        var policyGroup = endpoints.MapGroup("/api/v1/attendance/policies")
            .WithTags("Attendance Policies")
            .RequireAuthorization(AuthorizationPolicies.AttendancePolicyManagement);

        policyGroup.MapPut("/{academicSessionId:guid}", async (Guid academicSessionId, ConfigureAttendancePolicyRequest request, HttpContext http, AttendanceReportingService service, CancellationToken ct) =>
            Results.Ok(await service.ConfigurePolicyAsync(academicSessionId, request, GetActorId(http.User), ct)))
            .WithName("ConfigureAttendancePolicy");

        policyGroup.MapGet("/{academicSessionId:guid}", async (Guid academicSessionId, AttendanceReportingService service, CancellationToken ct) =>
            Results.Ok(await service.GetPolicyAsync(academicSessionId, ct)))
            .WithName("GetAttendancePolicy");

        group.MapGet("/offerings/{offeringId:guid}/report", async (Guid offeringId, HttpContext http, AttendanceReportingService service, CancellationToken ct) =>
            Results.Ok(await service.GetOfferingReportAsync(offeringId, GetActorId(http.User), IsPrivileged(http.User), ct)))
            .WithName("GetCourseAttendanceReport");

        return endpoints;
    }

    private static Guid GetActorId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("The authenticated account identifier is missing or invalid.");
    }

    private static bool IsPrivileged(ClaimsPrincipal principal) =>
        principal.IsInRole("Registrar") || principal.IsInRole("DepartmentAdmin") || principal.IsInRole("SuperAdmin");
}
