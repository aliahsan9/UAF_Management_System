using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Attendance.Abstractions;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly UafDbContext _db;
    public AttendanceRepository(UafDbContext db) => _db = db;

    public async Task<bool> CanManageOfferingAsync(Guid offeringId, Guid actorId, bool privileged, CancellationToken ct)
    {
        var offering = await _db.CourseOfferings.AsNoTracking()
            .Where(x => x.Id == offeringId && x.Status == CourseOfferingStatus.Published)
            .Select(x => new { x.Id, x.InstructorEmployeeId }).SingleOrDefaultAsync(ct);
        if (offering is null) return false;
        if (privileged) return true;
        if (!offering.InstructorEmployeeId.HasValue) return false;
        return await _db.Employees.AnyAsync(e => e.Id == offering.InstructorEmployeeId.Value
            && e.IdentityUserId == actorId && e.Status == EmployeeStatus.Active
            && e.Category == EmployeeCategory.Academic
            && _db.TeacherProfiles.Any(t => t.EmployeeId == e.Id), ct);
    }

    public Task<bool> SessionExistsForDateAsync(Guid offeringId, DateOnly date, CancellationToken ct) =>
        _db.AttendanceSessions.AnyAsync(x => x.CourseOfferingId == offeringId && x.AttendanceDate == date, ct);

    public Task AddSessionAsync(AttendanceSession session, CancellationToken ct) => _db.AttendanceSessions.AddAsync(session, ct).AsTask();

    public Task<AttendanceSession?> GetSessionAsync(Guid id, CancellationToken ct) =>
        _db.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<StudentAttendance>> GetMarksAsync(Guid id, CancellationToken ct) =>
        await _db.StudentAttendances.AsNoTracking().Where(x => x.AttendanceSessionId == id)
            .OrderBy(x => x.StudentId).ToListAsync(ct);

    private Task<AttendanceSession?> GetSessionForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.AttendanceSessions.FromSqlInterpolated($"SELECT * FROM [AttendanceSessions] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .FirstOrDefaultAsync(ct);

    private async Task EnsureActorCanManageAsync(AttendanceSession session, Guid actorId, bool privileged, CancellationToken ct)
    {
        if (!await CanManageOfferingAsync(session.CourseOfferingId, actorId, privileged, ct))
            throw new UnauthorizedAccessException("You are not authorized to manage attendance for this course offering.");
    }

    public async Task MarkAsync(Guid sessionId, Guid actorId, bool privileged, IReadOnlyCollection<AttendanceMarkItem> marks, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await GetSessionForUpdateAsync(sessionId, ct) ?? throw new KeyNotFoundException("Attendance session was not found.");
        await EnsureActorCanManageAsync(session, actorId, privileged, ct);
        if (session.Status != AttendanceSessionStatus.Open) throw new InvalidOperationException("Attendance can only be changed while the session is open.");

        var requestedIds = marks.Select(x => x.StudentId).ToArray();
        var eligibleIds = await _db.CourseEnrollments
            .Where(x => x.CourseOfferingId == session.CourseOfferingId && x.Status == EnrollmentStatus.Enrolled
                && requestedIds.Contains(x.StudentId))
            .Select(x => x.StudentId).ToListAsync(ct);
        if (eligibleIds.Count != requestedIds.Length)
            throw new InvalidOperationException("Every student must have an active enrollment in this course offering.");

        var existing = await _db.StudentAttendances.Where(x => x.AttendanceSessionId == sessionId && requestedIds.Contains(x.StudentId)).ToListAsync(ct);
        var byStudent = existing.ToDictionary(x => x.StudentId);
        var now = DateTimeOffset.UtcNow;
        foreach (var mark in marks)
        {
            if (byStudent.TryGetValue(mark.StudentId, out var current))
            {
                if (current.Status == mark.Status) continue;
                current.Correct(mark.Status, actorId, now, mark.Reason ?? string.Empty);
                _db.AttendanceChangeAudits.Add(new AttendanceChangeAudit(sessionId, mark.StudentId, actorId,
                    current.Status == mark.Status ? mark.Status : existing.First(x => x.StudentId == mark.StudentId).Status,
                    mark.Status, mark.Reason, now));
            }
            else
            {
                var record = new StudentAttendance(sessionId, mark.StudentId, mark.Status, actorId, now, mark.Reason);
                _db.StudentAttendances.Add(record);
                _db.AttendanceChangeAudits.Add(new AttendanceChangeAudit(sessionId, mark.StudentId, actorId,
                    null, mark.Status, mark.Reason, now));
            }
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task FinalizeAsync(Guid sessionId, Guid actorId, bool privileged, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await GetSessionForUpdateAsync(sessionId, ct) ?? throw new KeyNotFoundException("Attendance session was not found.");
        await EnsureActorCanManageAsync(session, actorId, privileged, ct);
        if (session.Status != AttendanceSessionStatus.Open) throw new InvalidOperationException("Only an open attendance session can be finalized.");
        var enrolledIds = await _db.CourseEnrollments.Where(x => x.CourseOfferingId == session.CourseOfferingId && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => x.StudentId).ToListAsync(ct);
        var markedIds = await _db.StudentAttendances.Where(x => x.AttendanceSessionId == sessionId).Select(x => x.StudentId).ToListAsync(ct);
        if (enrolledIds.Count == 0 || enrolledIds.Except(markedIds).Any())
            throw new InvalidOperationException("All currently enrolled students must have an attendance status before finalization.");
        session.Finalize(actorId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
