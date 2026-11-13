using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Grading;
using UAF.Application.Features.Grading.Models;
namespace UAF.API.Endpoints;
public static class GradeAppealEndpoints
{
    public static IEndpointRouteBuilder MapGradeAppealEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var student = endpoints.MapGroup("/api/v1/academic-records/me/grade-appeals")
            .WithTags("Student Grade Appeals")
            .RequireAuthorization(AuthorizationPolicies.StudentAcademicRecord);
        student.MapPost("/", async (CreateGradeAppealRequest request, HttpContext http, GradeAppealService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(GetActorId(http.User), request, ct);
            return Results.Created("/api/v1/academic-records/me/grade-appeals", result);
        }).WithName("CreateMyGradeAppeal");
        student.MapGet("/", async (HttpContext http, GradeAppealService service, CancellationToken ct) =>
            Results.Ok(await service.GetMineAsync(GetActorId(http.User), ct)))
            .WithName("GetMyGradeAppeals");

        var staff = endpoints.MapGroup("/api/v1/grading/grade-appeals")
            .WithTags("Grade Appeal Review")
            .RequireAuthorization(AuthorizationPolicies.GradeAppealReview);
        staff.MapGet("/pending", async (HttpContext http, GradeAppealService service, CancellationToken ct) =>
            Results.Ok(await service.GetPendingAsync(GetActorId(http.User), IsPrivileged(http.User), http.User.IsInRole("DepartmentAdmin"), ct)))
            .WithName("GetPendingGradeAppeals");
        staff.MapGet("/{appealId:guid}/history", async (Guid appealId, HttpContext http, GradeAppealService service, CancellationToken ct) =>
            Results.Ok(await service.GetHistoryAsync(appealId, GetActorId(http.User), IsPrivileged(http.User),
                http.User.IsInRole("DepartmentAdmin"), ct)))
            .WithName("GetGradeAppealHistory");
        staff.MapPost("/{appealId:guid}/decision", async (Guid appealId, DecideGradeAppealRequest request, HttpContext http, GradeAppealService service, CancellationToken ct) =>
            Results.Ok(await service.DecideAsync(appealId, GetActorId(http.User), IsPrivileged(http.User),
                http.User.IsInRole("DepartmentAdmin"), request, ct)))
            .WithName("DecideGradeAppeal");
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
