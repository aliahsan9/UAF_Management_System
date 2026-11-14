using UAF.API.Authorization;
using UAF.Application.Features.Timetable;
using UAF.Application.Features.Timetable.Models;

namespace UAF.API.Endpoints;

public static class TimetableEndpoints
{
    public static IEndpointRouteBuilder MapTimetableEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/timetable")
            .WithTags("Timetable")
            .RequireAuthorization(AuthorizationPolicies.TimetableManagement);

        group.MapGet("/rooms", (Guid? campusId, TimetableService service, CancellationToken ct) =>
            service.GetRoomsAsync(campusId, ct))
            .WithName("GetTeachingRooms");

        group.MapPost("/rooms", async (CreateTeachingRoomRequest request, TimetableService service, CancellationToken ct) =>
        {
            var id = await service.CreateRoomAsync(request, ct);
            return Results.Created($"/api/v1/timetable/rooms/{id}", new { id });
        })
        .WithName("CreateTeachingRoom")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/entries", (Guid? academicSessionId, Guid? courseOfferingId, TimetableService service, CancellationToken ct) =>
            service.GetEntriesAsync(academicSessionId, courseOfferingId, ct))
            .WithName("GetTimetableEntries");

        group.MapPost("/entries", async (CreateTimetableEntryRequest request, TimetableService service, CancellationToken ct) =>
        {
            var id = await service.ScheduleAsync(request, ct);
            return Results.Created($"/api/v1/timetable/entries/{id}", new { id });
        })
        .WithName("CreateTimetableEntry")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/entries/{id:guid}", async (Guid id, TimetableService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, ct);
            return Results.NoContent();
        })
        .WithName("CancelTimetableEntry")
        .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
