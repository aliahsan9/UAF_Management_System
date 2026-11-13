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
        var finalizedSessions = await _db.AttendanceSessions.AsNoTracking()
            .Where(x => x.CourseOfferingId == offeringId && x.Status == AttendanceSessionStatus.Finalized)
            .Select(x => new { x.Id, x.AttendanceDate }).ToListAsync(ct);
        var finalizedSessionIds = finalizedSessions.Select(x => x.Id).ToArray();
        var roster = await (
            from enrollment in _db.CourseEnrollments.AsNoTracking()
            join student in _db.Students.AsNoTracking() on enrollment.StudentId equals student.Id
            where enrollment.CourseOfferingId == offeringId
                && enrollment.Status != EnrollmentStatus.Dropped
                && enrollment.Status != EnrollmentStatus.Withdrawn
            orderby student.RegistrationNumber
            select new { enrollment.StudentId, enrollment.Status, enrollment.EnrolledOn, student.RegistrationNumber, student.FullName }
        ).ToListAsync(ct);
        List<(Guid StudentId, AttendanceStatus Status, DateOnly AttendanceDate)> marks;
        if (finalizedSessionIds.Length == 0)
        {
            marks = new List<(Guid StudentId, AttendanceStatus Status, DateOnly AttendanceDate)>();
        }
        else
        {
            var markRows = await (
                from mark in _db.StudentAttendances.AsNoTracking()
                join attendanceSession in _db.AttendanceSessions.AsNoTracking()
                    on mark.AttendanceSessionId equals attendanceSession.Id
                where finalizedSessionIds.Contains(mark.AttendanceSessionId)
                select new { mark.StudentId, mark.Status, attendanceSession.AttendanceDate }
            ).ToListAsync(ct);
            marks = markRows.Select(x => (x.StudentId, x.Status, x.AttendanceDate)).ToList();
        }
                var students = roster.Select(x =>
        {
            var eligibleSessionCount = finalizedSessions.Count(session => session.AttendanceDate >= x.EnrolledOn);
            var studentMarks = marks.Where(mark => mark.StudentId == x.StudentId && mark.AttendanceDate >= x.EnrolledOn)
                .Select(mark => mark.Status).ToArray();
            return new AttendanceReportStudentSource(x.StudentId, x.RegistrationNumber, x.FullName, x.Status,
                eligibleSessionCount, studentMarks);
        }).ToArray();
        return new AttendanceReportSource(offeringId, offering.AcademicSessionId, finalizedSessionIds.Length, students);
    }
}
