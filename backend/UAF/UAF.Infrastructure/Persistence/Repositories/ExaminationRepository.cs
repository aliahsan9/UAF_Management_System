using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Examinations.Abstractions;
using UAF.Application.Features.Examinations.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class ExaminationRepository : IExaminationRepository
{
    private readonly UafDbContext _db;
    public ExaminationRepository(UafDbContext db) => _db = db;

    public async Task<bool> CanManageOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct, bool allowClosed = false)
    {
        var offering = await (
            from co in _db.CourseOfferings.AsNoTracking()
            join course in _db.Courses.AsNoTracking() on co.CourseId equals course.Id
            where co.Id == offeringId && (co.Status == CourseOfferingStatus.Published || (allowClosed && co.Status == CourseOfferingStatus.Closed))
            select new { co.InstructorEmployeeId, course.DepartmentId }
        ).SingleOrDefaultAsync(ct);
        if (offering is null) return false;
        if (privileged) return true;
        if (departmentAdmin)
            return await _db.Employees.AnyAsync(e => e.IdentityUserId == actorId && e.Status == EmployeeStatus.Active && e.DepartmentId == offering.DepartmentId, ct);
        if (!offering.InstructorEmployeeId.HasValue) return false;
        return await _db.Employees.AnyAsync(e => e.Id == offering.InstructorEmployeeId.Value && e.IdentityUserId == actorId
            && e.Status == EmployeeStatus.Active && e.Category == EmployeeCategory.Academic
            && _db.TeacherProfiles.Any(t => t.EmployeeId == e.Id), ct);
    }

    public async Task<bool> CanAdministerOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        if (privileged) return await _db.CourseOfferings.AnyAsync(x => x.Id == offeringId && x.Status == CourseOfferingStatus.Published, ct);
        if (!departmentAdmin) return false;
        var departmentId = await _db.Courses.Where(c => _db.CourseOfferings.Any(o => o.Id == offeringId && o.CourseId == c.Id && o.Status == CourseOfferingStatus.Published))
            .Select(c => c.DepartmentId).SingleOrDefaultAsync(ct);
        return departmentId != Guid.Empty && await _db.Employees.AnyAsync(e => e.IdentityUserId == actorId && e.Status == EmployeeStatus.Active && e.DepartmentId == departmentId, ct);
    }
    public Task AddAsync(Examination examination, CancellationToken ct) => _db.Examinations.AddAsync(examination, ct).AsTask();
    public Task<Examination?> GetAsync(Guid id, CancellationToken ct) => _db.Examinations.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExistsAsync(Guid offeringId, string title, CancellationToken ct) =>
        _db.Examinations.AnyAsync(x => x.CourseOfferingId == offeringId && x.Title == title.Trim(), ct);

    public async Task SaveMarksAsync(Guid examinationId, Guid actorId, IReadOnlyCollection<ExaminationMarkItem> marks, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var exam = await _db.Examinations.SingleOrDefaultAsync(x => x.Id == examinationId, ct)
            ?? throw new KeyNotFoundException("Examination not found.");
        if (exam.Status != ExaminationStatus.MarksEntryOpen) throw new InvalidOperationException("Marks entry is not open.");
        if (!await _db.CourseOfferings.AnyAsync(x => x.Id == exam.CourseOfferingId && x.Status == CourseOfferingStatus.Published, ct))
            throw new InvalidOperationException("Marks can only be entered for a published course offering.");
        var enrolledIds = await _db.CourseEnrollments.Where(x => x.CourseOfferingId == exam.CourseOfferingId && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => x.StudentId).ToArrayAsync(ct);
        var requestedIds = marks.Select(x => x.StudentId).ToArray();
        if (requestedIds.Except(enrolledIds).Any()) throw new ArgumentException("Marks can only be recorded for currently enrolled students.");
        var existing = await _db.ExaminationResults.Where(x => x.ExaminationId == examinationId && requestedIds.Contains(x.StudentId)).ToDictionaryAsync(x => x.StudentId, ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var item in marks)
        {
            if (item.MarksObtained > exam.MaximumMarks) throw new ArgumentOutOfRangeException(nameof(marks), "Marks exceed the examination maximum.");
            if (existing.TryGetValue(item.StudentId, out var result))
            {
                var audit = new ExaminationResultAudit(examinationId, item.StudentId, result.MarksObtained, item.MarksObtained,
                    result.IsAbsent, item.IsAbsent, item.Reason, actorId, now);
                result.Update(item.MarksObtained, item.IsAbsent, item.Remarks, actorId, now);
                _db.ExaminationResultAudits.Add(audit);
            }
            else
            {
                var result = new ExaminationResult(examinationId, item.StudentId, item.MarksObtained, item.IsAbsent, item.Remarks, actorId);
                _db.ExaminationResults.Add(result);
                _db.ExaminationResultAudits.Add(new ExaminationResultAudit(examinationId, item.StudentId, null, item.MarksObtained,
                    false, item.IsAbsent, item.Reason, actorId, now));
            }
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task SubmitAsync(Guid examinationId, Guid actorId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var exam = await _db.Examinations.SingleOrDefaultAsync(x => x.Id == examinationId, ct)
            ?? throw new KeyNotFoundException("Examination not found.");
        if (!await _db.CourseOfferings.AnyAsync(x => x.Id == exam.CourseOfferingId && x.Status == CourseOfferingStatus.Published, ct))
            throw new InvalidOperationException("Results can only be submitted for a published course offering.");
        var enrolledIds = await _db.CourseEnrollments.Where(x => x.CourseOfferingId == exam.CourseOfferingId && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => x.StudentId).ToArrayAsync(ct);
        var markedIds = await _db.ExaminationResults.Where(x => x.ExaminationId == examinationId).Select(x => x.StudentId).ToArrayAsync(ct);
        if (enrolledIds.Except(markedIds).Any()) throw new InvalidOperationException("Every currently enrolled student must have a marks record before submission. Record absences as zero marks.");
        exam.Submit(actorId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApproveAsync(Guid examinationId, Guid actorId, CancellationToken ct)
    {
        var exam = await _db.Examinations.SingleOrDefaultAsync(x => x.Id == examinationId, ct) ?? throw new KeyNotFoundException("Examination not found.");
        exam.Approve(actorId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);
    }
    public async Task PublishAsync(Guid examinationId, Guid actorId, CancellationToken ct)
    {
        var exam = await _db.Examinations.SingleOrDefaultAsync(x => x.Id == examinationId, ct) ?? throw new KeyNotFoundException("Examination not found.");
        exam.Publish(actorId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<ExaminationResultView>> GetResultsAsync(Guid id, CancellationToken ct) =>
        await (from result in _db.ExaminationResults.AsNoTracking()
               join student in _db.Students.AsNoTracking() on result.StudentId equals student.Id
               join exam in _db.Examinations.AsNoTracking() on result.ExaminationId equals exam.Id
               where result.ExaminationId == id
               orderby student.RegistrationNumber
               select new ExaminationResultView(result.StudentId, student.RegistrationNumber, student.FullName,
                   result.MarksObtained, result.IsAbsent, !result.IsAbsent && result.MarksObtained >= exam.PassingMarks, result.Remarks)).ToListAsync(ct);
    public async Task<IReadOnlyList<ExaminationListItem>> GetOfferingExaminationsAsync(Guid offeringId, CancellationToken ct) =>
        await _db.Examinations.AsNoTracking().Where(x => x.CourseOfferingId == offeringId).OrderBy(x => x.ScheduledDate)
            .Select(x => new ExaminationListItem(x.Id, x.Title, x.Type, x.ScheduledDate, x.MaximumMarks, x.PassingMarks, x.Status)).ToListAsync(ct);
}
