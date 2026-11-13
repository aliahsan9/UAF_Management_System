using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Attendance.Abstractions;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class AttendanceReportingRepository : IAttendanceReportingRepository
{
    private readonly UafDbContext _db;
    public AttendanceReportingRepository(UafDbContext db) => _db = db;
    public Task<bool> AcademicSessionExistsAsync(Guid id, CancellationToken ct) =>
        _db.AcademicSessions.AnyAsync(x => x.Id == id, ct);
    public Task<AttendancePolicy?> GetPolicyAsync(Guid id, CancellationToken ct) =>
        _db.AttendancePolicies.FirstOrDefaultAsync(x => x.AcademicSessionId == id, ct);
    public Task AddPolicyAsync(AttendancePolicy policy, CancellationToken ct) => _db.AttendancePolicies.AddAsync(policy, ct).AsTask();

    public async Task<AttendanceReportSource?> GetReportSourceAsync(Guid offeringId, CancellationToken ct)
    {
        var offering = await _db.CourseOfferings.AsNoTracking()
            .Where(x => x.Id == offeringId)
            .Select(x => new { x.Id, x.AcademicSessionId }).SingleOrDefaultAsync(ct);
        if (offering is null) return null;
        var finalizedSessionIds = await _db.AttendanceSessions.AsNoTracking()
            .Where(x => x.CourseOfferingId == offeringId && x.Status == AttendanceSessionStatus.Finalized)
            .Select(x => x.Id).ToArrayAsync(ct);
        var roster = await _db.CourseEnrollments.AsNoTracking()
            .Where(x => x.CourseOfferingId == offeringId && x.Status != EnrollmentStatus.Dropped && x.Status != EnrollmentStatus.Withdrawn)
            .Select(x => new { x.StudentId, x.Status, x.Student.RegistrationNumber, x.Student.FullName })
            .OrderBy(x => x.RegistrationNumber).ToListAsync(ct);
        List<(Guid StudentId, AttendanceStatus Status)> marks;
        if (finalizedSessionIds.Length == 0)
        {
            marks = new List<(Guid StudentId, AttendanceStatus Status)>();
        }
        else
        {
            var markRows = await _db.StudentAttendances.AsNoTracking()
                .Where(x => finalizedSessionIds.Contains(x.AttendanceSessionId))
                .Select(x => new { x.StudentId, x.Status }).ToListAsync(ct);
            marks = markRows.Select(x => (x.StudentId, x.Status)).ToList();
        }
        var byStudent = marks.GroupBy(x => x.StudentId).ToDictionary(g => g.Key, g => (IReadOnlyList<AttendanceStatus>)g.Select(x => x.Status).ToArray());
        var students = roster.Select(x => new AttendanceReportStudentSource(x.StudentId, x.RegistrationNumber, x.FullName, x.Status,
            byStudent.TryGetValue(x.StudentId, out var values) ? values : Array.Empty<AttendanceStatus>())).ToArray();
        return new AttendanceReportSource(offeringId, offering.AcademicSessionId, finalizedSessionIds.Length, students);
    }
}
