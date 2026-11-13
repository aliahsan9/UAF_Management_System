using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class GradeRevisionRepository : IGradeRevisionRepository
{
    private readonly UafDbContext _db;
    public GradeRevisionRepository(UafDbContext db) => _db = db;
    public Task<Guid?> GetStudentIdForIdentityUserAsync(Guid userId, CancellationToken ct) =>
        _db.Students.Where(x => x.IdentityUserId == userId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    public async Task<bool> CanRequestRevisionAsync(Guid gradeId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var departmentId = await (from grade in _db.StudentCourseGrades.AsNoTracking()
            join course in _db.Courses.AsNoTracking() on grade.CourseId equals course.Id
            where grade.Id == gradeId select (Guid?)course.DepartmentId).SingleOrDefaultAsync(ct);
        if (!departmentId.HasValue) return false;
        if (privileged) return true;
        return departmentAdmin && await _db.Employees.AnyAsync(e => e.IdentityUserId == actorId &&
            e.Status == EmployeeStatus.Active && e.DepartmentId == departmentId.Value, ct);
    }
    public async Task<GradeRevisionContext?> GetRevisionContextAsync(Guid gradeId, CancellationToken ct)
    {
        var grade = await _db.StudentCourseGrades.AsNoTracking().SingleOrDefaultAsync(x => x.Id == gradeId, ct);
        if (grade is null) return null;
        var scale = await _db.GradeScales.Include("_bands").AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == grade.GradeScaleId, ct);
        if (scale is null) return null;
        var latest = await _db.GradeRevisions.AsNoTracking().Where(x => x.StudentCourseGradeId == gradeId)
            .OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct);
        return new GradeRevisionContext(grade, scale, latest);
    }
    public Task<bool> ApprovedAppealExistsAsync(Guid appealId, Guid gradeId, CancellationToken ct) =>
        _db.GradeAppeals.AnyAsync(x => x.Id == appealId && x.StudentCourseGradeId == gradeId && x.Status == GradeAppealStatus.Approved, ct);
    public Task<bool> HasPendingRequestAsync(Guid gradeId, CancellationToken ct) =>
        _db.GradeRevisionRequests.AnyAsync(x => x.StudentCourseGradeId == gradeId && x.Status == GradeRevisionStatus.PendingApproval, ct);
    public async Task AddRequestAsync(GradeRevisionRequest request, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var grade = await _db.StudentCourseGrades.SingleOrDefaultAsync(x => x.Id == request.StudentCourseGradeId, ct)
            ?? throw new KeyNotFoundException("Finalized course grade not found.");
        if (request.ProposedMarksObtained > grade.TotalMarks) throw new ArgumentOutOfRangeException(nameof(request.ProposedMarksObtained));
        if (!await _db.GradeAppeals.AnyAsync(x => x.Id == request.AppealId && x.StudentCourseGradeId == grade.Id && x.Status == GradeAppealStatus.Approved, ct))
            throw new InvalidOperationException("An approved appeal for this exact course grade is required.");
        if (await _db.GradeRevisionRequests.AnyAsync(x => x.StudentCourseGradeId == grade.Id && x.Status == GradeRevisionStatus.PendingApproval, ct))
            throw new InvalidOperationException("A pending grade revision request already exists.");
        _db.GradeRevisionRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<GradeRevisionRequestResponse>> GetPendingRequestsAsync(Guid actorId, bool privileged, CancellationToken ct)
    {
        var query = from request in _db.GradeRevisionRequests.AsNoTracking()
            join grade in _db.StudentCourseGrades.AsNoTracking() on request.StudentCourseGradeId equals grade.Id
            join student in _db.Students.AsNoTracking() on grade.StudentId equals student.Id
            where request.Status == GradeRevisionStatus.PendingApproval
            let latestMarks = _db.GradeRevisions.Where(revision => revision.StudentCourseGradeId == grade.Id)
                .OrderByDescending(revision => revision.RevisionNumber)
                .Select(revision => (decimal?)revision.NewMarks).FirstOrDefault()
            select new { request, grade, student, currentMarks = latestMarks ?? grade.MarksObtained };
        if (!privileged)
        {
            var departmentId = await _db.Employees.Where(e => e.IdentityUserId == actorId && e.Status == EmployeeStatus.Active)
                .Select(e => (Guid?)e.DepartmentId).SingleOrDefaultAsync(ct);
            if (!departmentId.HasValue) return Array.Empty<GradeRevisionRequestResponse>();
            query = query.Where(x => _db.Courses.Where(c => c.Id == x.grade.CourseId)
                .Any(c => c.DepartmentId == departmentId.Value));
        }
        return await query.OrderBy(x => x.request.RequestedAtUtc)
            .Select(x => new GradeRevisionRequestResponse(x.request.Id, x.grade.Id, x.request.AppealId,
                x.student.Id, x.student.RegistrationNumber, x.grade.CourseCode, x.grade.CourseTitle,
                x.currentMarks, x.request.ProposedMarksObtained, x.request.Reason, x.request.Status,
                x.request.RequestedByUserId, x.request.RequestedAtUtc, x.request.ReviewedByUserId,
                x.request.ReviewedAtUtc, x.request.ReviewRemarks)).ToListAsync(ct);
    }
    public Task<GradeRevisionRequest?> GetRequestAsync(Guid requestId, CancellationToken ct) =>
        _db.GradeRevisionRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct);
    public async Task DecideRequestAsync(Guid requestId, Guid actorId, bool approve, string remarks, GradeRevision? revision,
        GradeRevisionAudit audit, int expectedRevisionNumber, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await _db.GradeRevisionRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct)
            ?? throw new KeyNotFoundException("Grade revision request not found.");
        if (request.Status != GradeRevisionStatus.PendingApproval) throw new InvalidOperationException("This request has already been decided.");
        if (request.RequestedByUserId == actorId) throw new UnauthorizedAccessException("The requesting officer cannot decide their own request.");
        if (approve)
        {
            var grade = await _db.StudentCourseGrades.SingleOrDefaultAsync(x => x.Id == request.StudentCourseGradeId, ct)
                ?? throw new KeyNotFoundException("Finalized course grade not found.");
            if (!await _db.GradeAppeals.AnyAsync(x => x.Id == request.AppealId && x.StudentCourseGradeId == grade.Id && x.Status == GradeAppealStatus.Approved, ct))
                throw new InvalidOperationException("The linked appeal is no longer approved.");
            var currentRevisionNumber = await _db.GradeRevisions.Where(x => x.StudentCourseGradeId == grade.Id)
                .Select(x => (int?)x.RevisionNumber).MaxAsync(ct) ?? 0;
            if (currentRevisionNumber != expectedRevisionNumber)
                throw new DbUpdateConcurrencyException("The grade was revised after this approval was prepared. Reload and retry.");
            if (revision is null || revision.RequestId != request.Id || revision.StudentCourseGradeId != grade.Id ||
                revision.RevisionNumber != currentRevisionNumber + 1 || revision.NewMarks != request.ProposedMarksObtained)
                throw new InvalidOperationException("The proposed revision snapshot is inconsistent with the request.");
            _db.GradeRevisions.Add(revision);
        }
        request.Decide(approve, actorId, remarks, DateTimeOffset.UtcNow);
        _db.GradeRevisionAudits.Add(audit);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public Task<bool> StudentOwnsGradeAsync(Guid gradeId, Guid studentId, CancellationToken ct) =>
        _db.StudentCourseGrades.AnyAsync(x => x.Id == gradeId && x.StudentId == studentId, ct);
    public async Task<IReadOnlyList<GradeRevisionResponse>> GetRevisionHistoryAsync(Guid gradeId, CancellationToken ct) =>
        await _db.GradeRevisions.AsNoTracking().Where(x => x.StudentCourseGradeId == gradeId)
            .OrderBy(x => x.RevisionNumber)
            .Select(x => new GradeRevisionResponse(x.Id, x.StudentCourseGradeId, x.RevisionNumber,
                x.PreviousMarks, x.NewMarks, x.PreviousPercentage, x.NewPercentage, x.PreviousLetterGrade,
                x.NewLetterGrade, x.PreviousGradePoints, x.NewGradePoints, x.PreviousIsPassed, x.NewIsPassed,
                x.Reason, x.RequestedByUserId, x.ApprovedByUserId, x.ApprovedAtUtc)).ToListAsync(ct);
}
