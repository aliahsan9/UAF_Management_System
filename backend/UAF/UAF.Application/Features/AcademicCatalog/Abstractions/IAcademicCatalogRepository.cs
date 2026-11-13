using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.AcademicCatalog.Abstractions;

public interface IAcademicCatalogRepository
{
    Task<IReadOnlyList<AcademicSession>> GetSessionsAsync(AcademicSessionStatus? status, CancellationToken ct = default);
    Task<AcademicSession?> GetSessionByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> SessionCodeExistsAsync(string code, Guid? excludingId = null, CancellationToken ct = default);
    Task AddAsync(AcademicSession session, CancellationToken ct = default);

    Task<IReadOnlyList<Course>> GetCoursesAsync(Guid? departmentId, bool? isActive, CancellationToken ct = default);
    Task<Course?> GetCourseByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> DepartmentIsActiveAsync(Guid departmentId, CancellationToken ct = default);
    Task<bool> CourseCodeExistsAsync(Guid departmentId, string code, Guid? excludingId = null, CancellationToken ct = default);
    Task<bool> CourseHasOpenOfferingsAsync(Guid courseId, CancellationToken ct = default);
    Task AddAsync(Course course, CancellationToken ct = default);
}