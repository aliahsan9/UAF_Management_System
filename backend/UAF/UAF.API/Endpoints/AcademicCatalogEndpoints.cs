using UAF.API.Authorization;
using UAF.Application.Features.AcademicCatalog;
using UAF.Application.Features.AcademicCatalog.Models;
using UAF.Domain.Enums;

namespace UAF.API.Endpoints;

public static class AcademicCatalogEndpoints
{
    public static IEndpointRouteBuilder MapAcademicCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sessions = endpoints.MapGroup("/api/v1/academic-sessions")
            .WithTags("Academic Sessions")
            .RequireAuthorization(AuthorizationPolicies.AcademicCatalogManagement);

        sessions.MapGet("/", (AcademicSessionStatus? status, AcademicCatalogService service, CancellationToken ct) =>
            service.GetSessionsAsync(status, ct)).WithName("GetAcademicSessions");
        sessions.MapGet("/{id:guid}", (Guid id, AcademicCatalogService service, CancellationToken ct) =>
            service.GetSessionAsync(id, ct)).WithName("GetAcademicSession");
        sessions.MapPost("/", async (CreateAcademicSessionRequest request, AcademicCatalogService service, CancellationToken ct) =>
        {
            var id = await service.CreateSessionAsync(request, ct);
            return Results.Created($"/api/v1/academic-sessions/{id}", new { id });
        }).WithName("CreateAcademicSession").ProducesProblem(StatusCodes.Status409Conflict);
        sessions.MapPut("/{id:guid}", async (Guid id, UpdateAcademicSessionRequest request, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.UpdateSessionAsync(id, request, ct);
            return Results.NoContent();
        }).WithName("UpdateAcademicSession");
        sessions.MapPost("/{id:guid}/activate", async (Guid id, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.ActivateSessionAsync(id, ct);
            return Results.NoContent();
        }).WithName("ActivateAcademicSession");
        sessions.MapPost("/{id:guid}/complete", async (Guid id, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.CompleteSessionAsync(id, ct);
            return Results.NoContent();
        }).WithName("CompleteAcademicSession");
        sessions.MapPost("/{id:guid}/cancel", async (Guid id, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.CancelSessionAsync(id, ct);
            return Results.NoContent();
        }).WithName("CancelAcademicSession");

        var courses = endpoints.MapGroup("/api/v1/courses")
            .WithTags("Courses")
            .RequireAuthorization(AuthorizationPolicies.AcademicCatalogManagement);

        courses.MapGet("/", (Guid? departmentId, bool? isActive, AcademicCatalogService service, CancellationToken ct) =>
            service.GetCoursesAsync(departmentId, isActive, ct)).WithName("GetCourses");
        courses.MapGet("/{id:guid}", (Guid id, AcademicCatalogService service, CancellationToken ct) =>
            service.GetCourseAsync(id, ct)).WithName("GetCourse");
        courses.MapPost("/", async (CreateCourseRequest request, AcademicCatalogService service, CancellationToken ct) =>
        {
            var id = await service.CreateCourseAsync(request, ct);
            return Results.Created($"/api/v1/courses/{id}", new { id });
        }).WithName("CreateCourse").ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        courses.MapPut("/{id:guid}", async (Guid id, UpdateCourseRequest request, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.UpdateCourseAsync(id, request, ct);
            return Results.NoContent();
        }).WithName("UpdateCourse");
        courses.MapDelete("/{id:guid}", async (Guid id, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.DeactivateCourseAsync(id, ct);
            return Results.NoContent();
        }).WithName("DeactivateCourse").ProducesProblem(StatusCodes.Status409Conflict);
        courses.MapPost("/{id:guid}/reactivate", async (Guid id, AcademicCatalogService service, CancellationToken ct) =>
        {
            await service.ReactivateCourseAsync(id, ct);
            return Results.NoContent();
        }).WithName("ReactivateCourse");

        return endpoints;
    }
}