using UAF.Domain.Entities;

namespace UAF.Application.Features.Timetable.Abstractions;

public interface ITimetableRepository
{
    Task<bool> CampusIsActiveAsync(Guid campusId, CancellationToken ct);
    Task<bool> RoomCodeExistsAsync(Guid campusId, string code, CancellationToken ct);
    Task AddRoomAsync(TeachingRoom room, CancellationToken ct);
    Task<IReadOnlyList<TeachingRoom>> GetRoomsAsync(Guid? campusId, CancellationToken ct);
    Task<TeachingRoom?> GetRoomAsync(Guid id, CancellationToken ct);
    Task<CourseOffering?> GetOfferingAsync(Guid id, CancellationToken ct);
    Task<AcademicSession?> GetSessionAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TimetableEntry>> GetEntriesAsync(Guid? sessionId, Guid? offeringId, CancellationToken ct);
    Task AddEntryWithConflictCheckAsync(TimetableEntry entry, CancellationToken ct);
    Task<TimetableEntry?> GetEntryAsync(Guid id, CancellationToken ct);
}
