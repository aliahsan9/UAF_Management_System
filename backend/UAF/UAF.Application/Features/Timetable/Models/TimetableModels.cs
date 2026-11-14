using UAF.Domain.Enums;

namespace UAF.Application.Features.Timetable.Models;

public sealed record CreateTeachingRoomRequest(Guid CampusId, string Code, string Name, int Capacity, string RoomType);
public sealed record TeachingRoomResponse(Guid Id, Guid CampusId, string Code, string Name, int Capacity, string RoomType, bool IsActive);
public sealed record CreateTimetableEntryRequest(Guid CourseOfferingId, Guid RoomId, DayOfWeek DayOfWeek,
    TimeOnly StartTime, TimeOnly EndTime, DateOnly EffectiveFrom, DateOnly EffectiveTo);
public sealed record TimetableEntryResponse(Guid Id, Guid CourseOfferingId, Guid RoomId, DayOfWeek DayOfWeek,
    TimeOnly StartTime, TimeOnly EndTime, DateOnly EffectiveFrom, DateOnly EffectiveTo, TimetableEntryStatus Status);
