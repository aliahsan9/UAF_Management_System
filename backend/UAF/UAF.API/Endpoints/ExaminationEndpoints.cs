using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Examinations;
using UAF.Application.Features.Examinations.Models;
namespace UAF.API.Endpoints;
public static class ExaminationEndpoints
{
    public static IEndpointRouteBuilder MapExaminationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/examinations")
            .WithTags("Examinations")
            .RequireAuthorization(AuthorizationPolicies.ExaminationManagement);

        group.MapPost("/", async (CreateExaminationRequest request, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            var id = await service.CreateAsync(request, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct);
            return Results.Created($"/api/v1/examinations/{id}", new { id });
        }).WithName("CreateExamination");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct)))
            .WithName("GetExamination");

        group.MapGet("/offerings/{offeringId:guid}", async (Guid offeringId, HttpContext http, ExaminationService service, CancellationToken ct) =>
            Results.Ok(await service.GetOfferingExaminationsAsync(offeringId, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct)))
            .WithName("GetOfferingExaminations");

        group.MapPost("/{id:guid}/open-marks-entry", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            await service.OpenMarksEntryAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct);
            return Results.NoContent();
        }).WithName("OpenExaminationMarksEntry");

        group.MapPut("/{id:guid}/marks", async (Guid id, SaveExaminationMarksRequest request, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            await service.SaveMarksAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), request, ct);
            return Results.NoContent();
        }).WithName("SaveExaminationMarks");

        group.MapPost("/{id:guid}/submit", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            await service.SubmitAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct);
            return Results.NoContent();
        }).WithName("SubmitExaminationResults");

        group.MapGet("/{id:guid}/results", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
            Results.Ok(await service.GetResultsAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct)))
            .WithName("GetPublishedExaminationResults");

        var approval = endpoints.MapGroup("/api/v1/examinations")
            .WithTags("Examination Approval")
            .RequireAuthorization(AuthorizationPolicies.ExaminationApproval);

        approval.MapPost("/{id:guid}/approve", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            await service.ApproveAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct);
            return Results.NoContent();
        }).WithName("ApproveExaminationResults");

        approval.MapPost("/{id:guid}/publish", async (Guid id, HttpContext http, ExaminationService service, CancellationToken ct) =>
        {
            await service.PublishAsync(id, GetActorId(http.User), IsPrivileged(http.User), IsDepartmentAdmin(http.User), ct);
            return Results.NoContent();
        }).WithName("PublishExaminationResults");

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
        principal.IsInRole("Registrar") || principal.IsInRole("SuperAdmin");
    private static bool IsDepartmentAdmin(ClaimsPrincipal principal) => principal.IsInRole("DepartmentAdmin");
}
