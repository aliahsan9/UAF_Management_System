using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class GradingRepository : IGradingRepository
{
    private readonly UafDbContext _db;
    public GradingRepository(UafDbContext db) => _db = db;
    public Task<bool> AcademicSessionExistsAsync(Guid id, CancellationToken ct) =>
        _db.AcademicSessions.AnyAsync(x => x.Id == id, ct);
    public Task<GradeScale?> GetScaleForSessionAsync(Guid id, CancellationToken ct) =>
        _db.GradeScales.Include("_bands")
            .SingleOrDefaultAsync(x => x.AcademicSessionId == id, ct);
    public Task<GradeScale?> GetScaleAsync(Guid id, CancellationToken ct) =>
        _db.GradeScales.Include("_bands")
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddScaleAsync(GradeScale scale, CancellationToken ct) => _db.GradeScales.AddAsync(scale, ct).AsTask();
    public async Task<bool> CanFinalizeGradeAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct)
    {
        var departmentId = await (from offering in _db.CourseOfferings.AsNoTracking()
            join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
            where offering.Id == offeringId && (offering.Status == CourseOfferingStatus.Published || offering.Status == CourseOfferingStatus.Closed)
            select (Guid?)course.DepartmentId).SingleOrDefaultAsync(ct);
        if (!departmentId.HasValue) return false;
        if (privileged) return true;
        return departmentAdmin && await _db.Employees.AnyAsync(e => e.IdentityUserId == actorId
            && e.Status == EmployeeStatus.Active && e.DepartmentId == departmentId.Value, ct);
    }
    public Task<CourseGradeSource?> GetCourseGradeSourceAsync(Guid offeringId, Guid studentId, CancellationToken ct) =>
        (from enrollment in _db.CourseEnrollments.AsNoTracking()
         join offering in _db.CourseOfferings.AsNoTracking() on enrollment.CourseOfferingId equals offering.Id
         join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
         join student in _db.Students.AsNoTracking() on enrollment.StudentId equals student.Id
         where enrollment.CourseOfferingId == offeringId && enrollment.StudentId == studentId
            && (enrollment.Status == EnrollmentStatus.Enrolled || enrollment.Status == EnrollmentStatus.Completed)
            && (offering.Status == CourseOfferingStatus.Published || offering.Status == CourseOfferingStatus.Closed)
         orderby enrollment.EnrolledOn descending
         select new CourseGradeSource(student.Id, student.RegistrationNumber, student.FullName, offering.Id,
            course.Id, offering.AcademicSessionId, course.Code, course.Title, course.CreditHours)).FirstOrDefaultAsync(ct);
    public Task<bool> CourseGradeExistsAsync(Guid offeringId, Guid studentId, CancellationToken ct) =>
        _db.StudentCourseGrades.AnyAsync(x => x.CourseOfferingId == offeringId && x.StudentId == studentId, ct);
    public async Task FinalizeCourseGradeAsync(StudentCourseGrade grade, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var eligibleEnrollment = await _db.CourseEnrollments.AnyAsync(x =>
            x.StudentId == grade.StudentId && x.CourseOfferingId == grade.CourseOfferingId
            && (x.Status == EnrollmentStatus.Enrolled || x.Status == EnrollmentStatus.Completed), ct);
        if (!eligibleEnrollment) throw new InvalidOperationException("The student is no longer eligible for this course grade.");
        var offeringIsActiveOrClosed = await _db.CourseOfferings.AnyAsync(x => x.Id == grade.CourseOfferingId
            && (x.Status == CourseOfferingStatus.Published || x.Status == CourseOfferingStatus.Closed), ct);
        if (!offeringIsActiveOrClosed) throw new InvalidOperationException("Grades can only be finalized for a published or closed course offering.");
        if (await _db.StudentCourseGrades.AnyAsync(x => x.StudentId == grade.StudentId && x.CourseOfferingId == grade.CourseOfferingId, ct))
            throw new InvalidOperationException("A finalized course grade already exists for this student and offering.");
        _db.StudentCourseGrades.Add(grade);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct) =>
        _db.Students.Where(x => x.IdentityUserId == identityUserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    public async Task<StudentAcademicRecordSource?> GetAcademicRecordAsync(Guid studentId, CancellationToken ct)
    {
        var student = await _db.Students.AsNoTracking().Where(x => x.Id == studentId)
            .Select(x => new { x.Id, x.RegistrationNumber, x.FullName }).SingleOrDefaultAsync(ct);
        if (student is null) return null;
        var grades = await (from grade in _db.StudentCourseGrades.AsNoTracking()
            join session in _db.AcademicSessions.AsNoTracking() on grade.AcademicSessionId equals session.Id
            where grade.StudentId == studentId
            orderby session.StartDate, grade.CourseCode
            select new AcademicRecordCourseItem(grade.CourseId, grade.CourseOfferingId, grade.AcademicSessionId,
                session.Name, session.StartDate, grade.CourseCode, grade.CourseTitle, grade.CreditHours,
                grade.TotalMarks, grade.MarksObtained, grade.Percentage, grade.LetterGrade, grade.GradePoints,
                grade.IsPassed, grade.IncludeFailedCoursesInGpa, grade.RepeatCoursePolicy, grade.FinalizedAtUtc)).ToListAsync(ct);
        return new StudentAcademicRecordSource(student.Id, student.RegistrationNumber, student.FullName, grades);
    }
}
