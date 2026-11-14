using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Timetable.Abstractions;
using UAF.Application.Features.Timetable.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Timetable;

/// <summary>Campus room inventory and conflict-safe recurring timetable scheduling.</summary>
public sealed class TimetableService
{
    private readonly ITimetableRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TimetableService(ITimetableRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<TeachingRoomResponse>> GetRoomsAsync(Guid? campusId, CancellationToken ct)
    {
        if (campusId == Guid.Empty) throw new ArgumentException("Campus filter cannot be empty.", nameof(campusId));
        return (await _repository.GetRoomsAsync(campusId, ct)).Select(Map).ToArray();
    }

    public async Task<Guid> CreateRoomAsync(CreateTeachingRoomRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await _repository.CampusIsActiveAsync(request.CampusId, ct))
            throw new KeyNotFoundException("Active campus not found.");
        var code = NormalizeCode(request.Code);
        if (await _repository.RoomCodeExistsAsync(request.CampusId, code, ct))
            throw new InvalidOperationException($"Room code '{code}' already exists on this campus.");
        var room = new TeachingRoom(request.CampusId, code, request.Name, request.Capacity, request.RoomType);
        await _repository.AddRoomAsync(room, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return room.Id;
    }

    public async Task<IReadOnlyList<TimetableEntryResponse>> GetEntriesAsync(Guid? sessionId, Guid? offeringId, CancellationToken ct)
    {
        if (sessionId == Guid.Empty || offeringId == Guid.Empty)
            throw new ArgumentException("Optional filters cannot be empty GUIDs.");
        return (await _repository.GetEntriesAsync(sessionId, offeringId, ct)).Select(Map).ToArray();
    }

    public async Task<Guid> ScheduleAsync(CreateTimetableEntryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var offering = await _repository.GetOfferingAsync(request.CourseOfferingId, ct)
            ?? throw new KeyNotFoundException("Course offering not found.");
        if (offering.Status is CourseOfferingStatus.Cancelled or CourseOfferingStatus.Closed)
            throw new InvalidOperationException("A closed or cancelled course offering cannot be scheduled.");
        var session = await _repository.GetSessionAsync(offering.AcademicSessionId, ct)
            ?? throw new KeyNotFoundException("Academic session not found.");
        if (session.Status is AcademicSessionStatus.Completed or AcademicSessionStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled academic session cannot be scheduled.");
        if (request.EffectiveFrom < session.StartDate || request.EffectiveTo > session.EndDate)
            throw new ArgumentException("Timetable dates must be within the academic session date range.");
        if (request.EffectiveTo < request.EffectiveFrom)
            throw new ArgumentException("Timetable end date cannot precede start date.");
        var room = await _repository.GetRoomAsync(request.RoomId, ct)
            ?? throw new KeyNotFoundException("Teaching room not found.");
        if (!room.IsActive) throw new InvalidOperationException("Inactive rooms cannot be scheduled.");
        if (room.Capacity < offering.Capacity)
            throw new InvalidOperationException("Room capacity cannot be lower than the course offering capacity.");

        var entry = new TimetableEntry(request.CourseOfferingId, request.RoomId, request.DayOfWeek,
            request.StartTime, request.EndTime, request.EffectiveFrom, request.EffectiveTo);
        await _repository.AddEntryWithConflictCheckAsync(entry, ct);
        return entry.Id;
    }

    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid timetable entry ID is required.", nameof(id));
        var entry = await _repository.GetEntryAsync(id, ct) ?? throw new KeyNotFoundException("Timetable entry not found.");
        entry.Cancel();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 30)
            throw new ArgumentException("Room code is required and cannot exceed 30 characters.", nameof(code));
        return code.Trim().ToUpperInvariant();
    }

    private static TeachingRoomResponse Map(TeachingRoom room) =>
        new(room.Id, room.CampusId, room.Code, room.Name, room.Capacity, room.RoomType, room.IsActive);

    private static TimetableEntryResponse Map(TimetableEntry entry) =>
        new(entry.Id, entry.CourseOfferingId, entry.RoomId, entry.DayOfWeek,
            entry.StartTime, entry.EndTime, entry.EffectiveFrom, entry.EffectiveTo, entry.Status);
}
