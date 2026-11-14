using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Timetable.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class TimetableRepository : ITimetableRepository
{
    private readonly UafDbContext _db;
    public TimetableRepository(UafDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<bool> CampusIsActiveAsync(Guid campusId, CancellationToken ct) =>
        _db.Campuses.AnyAsync(x => x.Id == campusId && x.IsActive, ct);

    public Task<bool> RoomCodeExistsAsync(Guid campusId, string code, CancellationToken ct) =>
        _db.TeachingRooms.AnyAsync(x => x.CampusId == campusId && x.Code == code, ct);

    public Task AddRoomAsync(TeachingRoom room, CancellationToken ct) =>
        _db.TeachingRooms.AddAsync(room, ct).AsTask();

    public async Task<IReadOnlyList<TeachingRoom>> GetRoomsAsync(Guid? campusId, CancellationToken ct) =>
        await _db.TeachingRooms.AsNoTracking()
            .Where(x => !campusId.HasValue || x.CampusId == campusId.Value)
            .OrderBy(x => x.CampusId).ThenBy(x => x.Code).ToListAsync(ct);

    public Task<TeachingRoom?> GetRoomAsync(Guid id, CancellationToken ct) =>
        _db.TeachingRooms.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CourseOffering?> GetOfferingAsync(Guid id, CancellationToken ct) =>
        _db.CourseOfferings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AcademicSession?> GetSessionAsync(Guid id, CancellationToken ct) =>
        _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<TimetableEntry>> GetEntriesAsync(Guid? sessionId, Guid? offeringId, CancellationToken ct) =>
        await _db.TimetableEntries.AsNoTracking()
            .Where(x => (!offeringId.HasValue || x.CourseOfferingId == offeringId.Value) &&
                (!sessionId.HasValue || _db.CourseOfferings.Any(o =>
                    o.Id == x.CourseOfferingId && o.AcademicSessionId == sessionId.Value)))
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).ThenBy(x => x.RoomId)
            .ToListAsync(ct);

    public Task<TimetableEntry?> GetEntryAsync(Guid id, CancellationToken ct) =>
        _db.TimetableEntries.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddEntryWithConflictCheckAsync(TimetableEntry entry, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        // Lock the resource and offering rows. Serializable predicate reads below prevent
        // concurrent requests from publishing overlapping room/instructor/cohort allocations.
        var room = await _db.TeachingRooms
            .FromSqlInterpolated($"SELECT * FROM [TeachingRooms] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {entry.RoomId}")
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Teaching room not found.");
        var offering = await _db.CourseOfferings
            .FromSqlInterpolated($"SELECT * FROM [CourseOfferings] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {entry.CourseOfferingId}")
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Course offering not found.");
        var session = await _db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == offering.AcademicSessionId, ct)
            ?? throw new KeyNotFoundException("Academic session not found.");

        if (!room.IsActive) throw new InvalidOperationException("Inactive rooms cannot be scheduled.");
        if (room.Capacity < offering.Capacity)
            throw new InvalidOperationException("Room capacity cannot be lower than the course offering capacity.");
        if (offering.Status is CourseOfferingStatus.Cancelled or CourseOfferingStatus.Closed)
            throw new InvalidOperationException("A closed or cancelled course offering cannot be scheduled.");
        if (session.Status is AcademicSessionStatus.Completed or AcademicSessionStatus.Cancelled)
            throw new InvalidOperationException("The academic session is not open for scheduling.");
        if (entry.EffectiveFrom < session.StartDate || entry.EffectiveTo > session.EndDate)
            throw new InvalidOperationException("Timetable dates must be within the academic session.");

        if (offering.InstructorEmployeeId.HasValue &&
            !await _db.Employees.AnyAsync(x => x.Id == offering.InstructorEmployeeId.Value &&
                x.Status == EmployeeStatus.Active, ct))
            throw new InvalidOperationException("The assigned instructor is not an active employee.");

        var overlaps = _db.TimetableEntries.Where(x =>
            x.Status == TimetableEntryStatus.Published &&
            x.DayOfWeek == entry.DayOfWeek &&
            x.EffectiveFrom <= entry.EffectiveTo && entry.EffectiveFrom <= x.EffectiveTo &&
            x.StartTime < entry.EndTime && entry.StartTime < x.EndTime);

        if (await overlaps.AnyAsync(x => x.RoomId == entry.RoomId, ct))
            throw new InvalidOperationException("The selected room already has a timetable entry during this time.");
        if (await overlaps.AnyAsync(x => x.CourseOfferingId == entry.CourseOfferingId, ct))
            throw new InvalidOperationException("This course offering already has an overlapping timetable entry.");

        if (offering.InstructorEmployeeId.HasValue &&
            await overlaps.AnyAsync(x => _db.CourseOfferings.Any(o =>
                o.Id == x.CourseOfferingId && o.InstructorEmployeeId == offering.InstructorEmployeeId), ct))
            throw new InvalidOperationException("The assigned instructor already has a timetable entry during this time.");

        var enrolledStudentConflict =
            from existingEntry in overlaps
            join existingEnrollment in _db.CourseEnrollments
                on existingEntry.CourseOfferingId equals existingEnrollment.CourseOfferingId
            join newEnrollment in _db.CourseEnrollments
                on existingEnrollment.StudentId equals newEnrollment.StudentId
            where existingEnrollment.Status == EnrollmentStatus.Enrolled &&
                  newEnrollment.Status == EnrollmentStatus.Enrolled &&
                  newEnrollment.CourseOfferingId == entry.CourseOfferingId
            select existingEntry.Id;
        if (await enrolledStudentConflict.AnyAsync(ct))
            throw new InvalidOperationException("At least one enrolled student has a conflicting timetable entry.");

        _db.TimetableEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
