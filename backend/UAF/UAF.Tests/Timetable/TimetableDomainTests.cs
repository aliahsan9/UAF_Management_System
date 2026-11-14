using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Timetable;

public sealed class TimetableDomainTests
{
    [Fact]
    public void TeachingRoom_RequiresPositiveCapacityAndValidCampus()
    {
        Assert.Throws<ArgumentException>(() => new TeachingRoom(Guid.Empty, "R-01", "Room 1", 30, "Classroom"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TeachingRoom(Guid.NewGuid(), "R-01", "Room 1", 0, "Classroom"));
    }

    [Fact]
    public void TeachingRoom_NormalizesCodeAndCanBeDeactivated()
    {
        var room = new TeachingRoom(Guid.NewGuid(), " r-01 ", "Room 1", 30, "Classroom");
        Assert.Equal("R-01", room.Code);
        Assert.True(room.IsActive);

        room.Deactivate();

        Assert.False(room.IsActive);
    }

    [Fact]
    public void TimetableEntry_RejectsInvalidTimeAndDateRanges()
    {
        var offeringId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        Assert.Throws<ArgumentException>(() => new TimetableEntry(
            offeringId, roomId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 0), date, date));
        Assert.Throws<ArgumentException>(() => new TimetableEntry(
            offeringId, roomId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 0), date.AddDays(1), date));
    }

    [Fact]
    public void TimetableEntry_IsPublishedOnCreationAndCanBeCancelled()
    {
        var date = new DateOnly(2026, 9, 1);
        var entry = new TimetableEntry(Guid.NewGuid(), Guid.NewGuid(), DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(10, 0), date, date.AddMonths(3));

        Assert.Equal(TimetableEntryStatus.Published, entry.Status);

        entry.Cancel();
        entry.Cancel();

        Assert.Equal(TimetableEntryStatus.Cancelled, entry.Status);
    }
}
