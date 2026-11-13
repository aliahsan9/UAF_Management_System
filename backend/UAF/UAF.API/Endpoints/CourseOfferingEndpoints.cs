using UAF.API.Authorization;
using UAF.Application.Features.CourseOfferings;
using UAF.Application.Features.CourseOfferings.Models;
using UAF.Domain.Enums;

namespace UAF.API.Endpoints;

public static class CourseOfferingEndpoints
{
    public static IEndpointRouteBuilder MapCourseOfferingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/course-offerings")
            .WithTags("Course Offerings")
            .RequireAuthorization(AuthorizationPolicies.CourseOfferingManagement);

        group.MapGet("/", (Guid? academicSessionId, Guid? courseId, CourseOfferingStatus? status,
            CourseOfferingService service, CancellationToken ct) =>
            service.GetOfferingsAsync(academicSessionId, courseId, status, ct))
            .WithName("GetCourseOfferings");

        group.MapGet("/{id:guid}", (Guid id, CourseOfferingService service, CancellationToken ct) =>
            service.GetOfferingAsync(id, ct)).WithName("GetCourseOffering");

        group.MapPost("/", async (CreateCourseOfferingRequest request, CourseOfferingService service, CancellationToken ct) =>
        {
            var id = await service.CreateOfferingAsync(request, ct);
            return Results.Created($"/api/v1/course-offerings/{id}", new { id });
        }).WithName("CreateCourseOffering").ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCourseOfferingRequest request, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.UpdateOfferingAsync(id, request, ct);
            return Results.NoContent();
        }).WithName("UpdateCourseOffering");

        group.MapPost("/{id:guid}/publish", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.PublishAsync(id, ct);
            return Results.NoContent();
        }).WithName("PublishCourseOffering");

        group.MapPost("/{id:guid}/close", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.CloseAsync(id, ct);
            return Results.NoContent();
        }).WithName("CloseCourseOffering");

        group.MapPost("/{id:guid}/cancel", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, ct);
            return Results.NoContent();
        }).WithName("CancelCourseOffering");

        group.MapGet("/{id:guid}/enrollments", (Guid id, CourseOfferingService service, CancellationToken ct) =>
            service.GetEnrollmentsAsync(id, ct)).WithName("GetOfferingEnrollments");

        group.MapPost("/{id:guid}/enrollments/{studentId:guid}", async (Guid id, Guid studentId,
            CourseOfferingService service, CancellationToken ct) =>
        {
            var enrollmentId = await service.EnrollStudentAsync(studentId, id, DateOnly.FromDateTime(DateTime.UtcNow), ct);
            return Results.Created($"/api/v1/course-enrollments/{enrollmentId}", new { id = enrollmentId });
        }).WithName("EnrollStudentInCourseOffering").ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}

public static class CourseEnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapCourseEnrollmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/course-enrollments")
            .WithTags("Course Enrollments")
            .RequireAuthorization(AuthorizationPolicies.CourseOfferingManagement);

        group.MapPost("/{id:guid}/drop", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.DropEnrollmentAsync(id, ct);
            return Results.NoContent();
        }).WithName("DropCourseEnrollment");

        group.MapPost("/{id:guid}/withdraw", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.WithdrawEnrollmentAsync(id, ct);
            return Results.NoContent();
        }).WithName("WithdrawCourseEnrollment");

        group.MapPost("/{id:guid}/complete", async (Guid id, CourseOfferingService service, CancellationToken ct) =>
        {
            await service.CompleteEnrollmentAsync(id, ct);
            return Results.NoContent();
        }).WithName("CompleteCourseEnrollment");

        return endpoints;
    }
}