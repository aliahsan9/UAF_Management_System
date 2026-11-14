using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>A recurring weekly teaching slot, expressed in the campus local time zone.</summary>
public sealed class TimetableEntry : AuditableEntity
{
    private TimetableEntry() { }

    public TimetableEntry(Guid courseOfferingId, Guid roomId, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime, DateOnly effectiveFrom, DateOnly effectiveTo)
    {
        if (courseOfferingId == Guid.Empty) throw new ArgumentException("Course offering is required.", nameof(courseOfferingId));
        if (roomId == Guid.Empty) throw new ArgumentException("Room is required.", nameof(roomId));
        if (!Enum.IsDefined(dayOfWeek)) throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        if (endTime <= startTime) throw new ArgumentException("End time must be later than start time.");
        if (effectiveTo < effectiveFrom) throw new ArgumentException("Effective end date cannot precede start date.");
        CourseOfferingId = courseOfferingId;
        RoomId = roomId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Status = TimetableEntryStatus.Draft;
    }

    public Guid CourseOfferingId { get; private set; }
    public Guid RoomId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly EffectiveTo { get; private set; }
    public TimetableEntryStatus Status { get; private set; }

    public void Publish()
    {
        if (Status != TimetableEntryStatus.Draft) throw new InvalidOperationException("Only a draft timetable entry can be published.");
        Status = TimetableEntryStatus.Published;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status == TimetableEntryStatus.Cancelled) return;
        Status = TimetableEntryStatus.Cancelled;
        MarkUpdated();
    }
}
