using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Grading;
using UAF.Application.Features.Grading.Models;
namespace UAF.API.Endpoints;
public static class GradingEndpoints
{
    public static IEndpointRouteBuilder MapGradingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var scales = endpoints.MapGroup("/api/v1/grading/scales")
            .WithTags("Grading Scales")
            .RequireAuthorization(AuthorizationPolicies.GradingScaleManagement);
        scales.MapPost("/", async (CreateGradeScaleRequest request, HttpContext http, GradingService service, CancellationToken ct) =>
        {
            var result = await service.CreateScaleAsync(request, GetActorId(http.User), ct);
            return Results.Created($"/api/v1/grading/scales/sessions/{result.AcademicSessionId}", result);
        }).WithName("CreateGradeScale");
        scales.MapGet("/sessions/{academicSessionId:guid}", async (Guid academicSessionId, GradingService service, CancellationToken ct) =>
            Results.Ok(await service.GetScaleForSessionAsync(academicSessionId, ct)))
            .WithName("GetGradeScaleForAcademicSession");
        scales.MapPost("/{scaleId:guid}/publish", async (Guid scaleId, HttpContext http, GradingService service, CancellationToken ct) =>
            Results.Ok(await service.PublishScaleAsync(scaleId, GetActorId(http.User), ct)))
            .WithName("PublishGradeScale");

        var grades = endpoints.MapGroup("/api/v1/grading")
            .WithTags("Course Grade Finalization")
            .RequireAuthorization(AuthorizationPolicies.CourseGradeManagement);
        grades.MapPost("/offerings/{offeringId:guid}/students/{studentId:guid}/finalize", async (Guid offeringId, Guid studentId, HttpContext http, GradingService service, CancellationToken ct) =>
        {
            var result = await service.FinalizeCourseGradeFromAssessmentsAsync(offeringId, studentId, GetActorId(http.User),
                IsPrivileged(http.User), http.User.IsInRole("DepartmentAdmin"), ct);
            return Results.Ok(result);
        }).WithName("FinalizeStudentCourseGradeFromPublishedAssessments");

        var records = endpoints.MapGroup("/api/v1/academic-records")
            .WithTags("Student Academic Records")
            .RequireAuthorization(AuthorizationPolicies.StudentAcademicRecord);
        records.MapGet("/me", async (HttpContext http, GradingService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyAcademicRecordAsync(GetActorId(http.User), ct)))
            .WithName("GetMyAcademicRecord");
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
}
