using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.AcademicCatalog.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class AcademicCatalogRepository : IAcademicCatalogRepository
{
    private readonly UafDbContext _dbContext;

    public AcademicCatalogRepository(UafDbContext dbContext) =>
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyList<AcademicSession>> GetSessionsAsync(AcademicSessionStatus? status, CancellationToken ct = default) =>
        await _dbContext.AcademicSessions.AsNoTracking()
            .Where(x => !status.HasValue || x.Status == status.Value)
            .OrderByDescending(x => x.StartDate).ThenBy(x => x.Code).ToListAsync(ct);

    public Task<AcademicSession?> GetSessionByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.AcademicSessions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> SessionCodeExistsAsync(string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.AcademicSessions.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task AddAsync(AcademicSession session, CancellationToken ct = default) =>
        _dbContext.AcademicSessions.AddAsync(session, ct).AsTask();

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(Guid? departmentId, bool? isActive, CancellationToken ct = default) =>
        await _dbContext.Courses.AsNoTracking()
            .Where(x => (!departmentId.HasValue || x.DepartmentId == departmentId.Value) &&
                        (!isActive.HasValue || x.IsActive == isActive.Value))
            .OrderBy(x => x.Code).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Course?> GetCourseByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> DepartmentIsActiveAsync(Guid departmentId, CancellationToken ct = default) =>
        _dbContext.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive, ct);

    public Task<bool> CourseCodeExistsAsync(Guid departmentId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Courses.AnyAsync(x => x.DepartmentId == departmentId && x.Code == code &&
            (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> CourseHasOpenOfferingsAsync(Guid courseId, CancellationToken ct = default) =>
        _dbContext.CourseOfferings.AnyAsync(x => x.CourseId == courseId &&
            (x.Status == CourseOfferingStatus.Draft || x.Status == CourseOfferingStatus.Published), ct);

    public Task AddAsync(Course course, CancellationToken ct = default) =>
        _dbContext.Courses.AddAsync(course, ct).AsTask();
}