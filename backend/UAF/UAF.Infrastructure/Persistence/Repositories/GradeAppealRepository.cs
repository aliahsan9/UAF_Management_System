using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class GradeAppealRepository : IGradeAppealRepository
{
    private readonly UafDbContext _db;
    public GradeAppealRepository(UafDbContext db) => _db = db;
    public Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct) =>
        _db.Students.Where(x => x.IdentityUserId == identityUserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    public Task<bool> GradeBelongsToStudentAsync(Guid gradeId, Guid studentId, CancellationToken ct) =>
        _db.StudentCourseGrades.AnyAsync(x => x.Id == gradeId && x.StudentId == studentId, ct);
    public Task<bool> HasPendingAppealAsync(Guid gradeId, CancellationToken ct) =>
        _db.GradeAppeals.AnyAsync(x => x.StudentCourseGradeId == gradeId && x.Status == GradeAppealStatus.Pending, ct);
    public async Task AddAppealAsync(GradeAppeal appeal, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await _db.StudentCourseGrades.AnyAsync(x => x.Id == appeal.StudentCourseGradeId && x.StudentId == appeal.StudentId, ct))
            throw new KeyNotFoundException("Published course grade was not found for this student.");
        if (await _db.GradeAppeals.AnyAsync(x => x.StudentCourseGradeId == appeal.StudentCourseGradeId && x.Status == GradeAppealStatus.Pending, ct))
            throw new InvalidOperationException("A pending appeal already exists for this course grade.");
        _db.GradeAppeals.Add(appeal);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<GradeAppealResponse>> GetStudentAppealsAsync(Guid studentId, CancellationToken ct) =>
        await AppealQuery().Where(x => x.StudentId == studentId).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct);
    public Task<GradeAppeal?> GetAppealAsync(Guid appealId, CancellationToken ct) =>
        _db.GradeAppeals.SingleOrDefaultAsync(x => x.Id == appealId, ct);
    public async Task<bool> CanReviewAppealAsync(Guid appealId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var departmentId = await (from appeal in _db.GradeAppeals.AsNoTracking()
            join grade in _db.StudentCourseGrades.AsNoTracking() on appeal.StudentCourseGradeId equals grade.Id
            join course in _db.Courses.AsNoTracking() on grade.CourseId equals course.Id
            where appeal.Id == appealId
            select (Guid?)course.DepartmentId).SingleOrDefaultAsync(ct);
        if (!departmentId.HasValue) return false;
        if (privileged) return true;
        return departmentAdmin && await _db.Employees.AnyAsync(e => e.IdentityUserId == actorId
            && e.Status == EmployeeStatus.Active && e.DepartmentId == departmentId.Value, ct);
    }
    public async Task<IReadOnlyList<GradeAppealResponse>> GetPendingAppealsAsync(Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var query = AppealQuery().Where(x => x.Status == GradeAppealStatus.Pending);
        if (!privileged)
        {
            var departmentId = await _db.Employees.Where(e => e.IdentityUserId == actorId && e.Status == EmployeeStatus.Active)
                .Select(e => (Guid?)e.DepartmentId).SingleOrDefaultAsync(ct);
            if (!departmentAdmin || !departmentId.HasValue) return Array.Empty<GradeAppealResponse>();
            query = query.Where(x => _db.StudentCourseGrades.Where(g => g.Id == x.StudentCourseGradeId)
                .Join(_db.Courses, g => g.CourseId, c => c.Id, (g, c) => c.DepartmentId).Any(id => id == departmentId.Value));
        }
        return await query.OrderBy(x => x.SubmittedAtUtc).ToListAsync(ct);
    }
    public async Task DecideAppealAsync(GradeAppeal appeal, GradeAppealAudit audit, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var approve = appeal.Status == GradeAppealStatus.Approved;
        var reviewerId = appeal.ReviewedByUserId!.Value;
        var reviewRemarks = appeal.ReviewRemarks!;
        var reviewedAt = appeal.ReviewedAtUtc!.Value;
        _db.Entry(appeal).State = EntityState.Detached;
        var persisted = await _db.GradeAppeals.SingleOrDefaultAsync(x => x.Id == appeal.Id, ct)
            ?? throw new KeyNotFoundException("Grade appeal not found.");
        if (persisted.Status != GradeAppealStatus.Pending) throw new InvalidOperationException("This appeal has already been decided.");
        if (!await _db.StudentCourseGrades.AnyAsync(x => x.Id == appeal.StudentCourseGradeId && x.StudentId == appeal.StudentId, ct))
            throw new InvalidOperationException("The appealed course grade no longer exists.");
        persisted.Decide(approve, reviewerId, reviewRemarks, reviewedAt);
        _db.GradeAppealAudits.Add(audit);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    private IQueryable<GradeAppealResponse> AppealQuery() =>
        from appeal in _db.GradeAppeals.AsNoTracking()
        join student in _db.Students.AsNoTracking() on appeal.StudentId equals student.Id
        join grade in _db.StudentCourseGrades.AsNoTracking() on appeal.StudentCourseGradeId equals grade.Id
        join session in _db.AcademicSessions.AsNoTracking() on grade.AcademicSessionId equals session.Id
        select new GradeAppealResponse(appeal.Id, appeal.StudentCourseGradeId, appeal.StudentId, student.RegistrationNumber,
            grade.CourseCode, grade.CourseTitle, session.Name, appeal.Reason, appeal.Status, appeal.SubmittedAtUtc,
            appeal.ReviewedByUserId, appeal.ReviewedAtUtc, appeal.ReviewRemarks);
}
