using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Grading.Abstractions;
public sealed record CourseGradeSource(Guid StudentId, string RegistrationNumber, string FullName, Guid CourseOfferingId, Guid CourseId, Guid AcademicSessionId, string CourseCode, string CourseTitle, decimal CreditHours);
public sealed record StudentAcademicRecordSource(Guid StudentId, string RegistrationNumber, string FullName, IReadOnlyList<AcademicRecordCourseItem> CourseGrades);
public interface IGradingRepository
{
    Task<bool> AcademicSessionExistsAsync(Guid academicSessionId, CancellationToken ct);
    Task<GradeScale?> GetScaleForSessionAsync(Guid academicSessionId, CancellationToken ct);
    Task<GradeScale?> GetScaleAsync(Guid scaleId, CancellationToken ct);
    Task AddScaleAsync(GradeScale scale, CancellationToken ct);
    Task<bool> CanFinalizeGradeAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct);
    Task<CourseGradeSource?> GetCourseGradeSourceAsync(Guid offeringId, Guid studentId, CancellationToken ct);
    Task<bool> CourseGradeExistsAsync(Guid offeringId, Guid studentId, CancellationToken ct);
    Task AddCourseGradeAsync(StudentCourseGrade grade, CancellationToken ct);
    Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct);
    Task<StudentAcademicRecordSource?> GetAcademicRecordAsync(Guid studentId, CancellationToken ct);
}
