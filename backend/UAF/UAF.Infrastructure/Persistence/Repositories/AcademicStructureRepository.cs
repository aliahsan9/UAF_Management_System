using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.AcademicStructure.Abstractions;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core persistence adapter for the university academic hierarchy.
/// </summary>
internal sealed class AcademicStructureRepository : IAcademicStructureRepository
{
    private readonly UafDbContext _dbContext;

    public AcademicStructureRepository(UafDbContext dbContext) =>
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public Task<bool> UniversityExistsAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Universities.AnyAsync(x => x.Id == id, ct);

    public Task<bool> FacultyExistsAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Faculties.AnyAsync(x => x.Id == id && x.IsActive, ct);

    public Task<bool> DepartmentExistsAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Departments.AnyAsync(x => x.Id == id && x.IsActive, ct);

    public Task<bool> IsUniversityShortNameInUseAsync(string shortName, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Universities.AnyAsync(x => x.ShortName == shortName && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> IsCampusCodeInUseAsync(Guid universityId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Campuses.AnyAsync(x => x.UniversityId == universityId && x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> IsFacultyCodeInUseAsync(Guid universityId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Faculties.AnyAsync(x => x.UniversityId == universityId && x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> IsDepartmentCodeInUseAsync(Guid facultyId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Departments.AnyAsync(x => x.FacultyId == facultyId && x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> IsProgramCodeInUseAsync(Guid departmentId, string code, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AnyAsync(x => x.DepartmentId == departmentId && x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public async Task<IReadOnlyList<University>> GetUniversitiesAsync(CancellationToken ct = default) =>
        await _dbContext.Universities.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Campus>> GetCampusesAsync(Guid? universityId = null, CancellationToken ct = default) =>
        await _dbContext.Campuses.AsNoTracking().Where(x => !universityId.HasValue || x.UniversityId == universityId.Value)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Faculty>> GetFacultiesAsync(Guid? universityId = null, CancellationToken ct = default) =>
        await _dbContext.Faculties.AsNoTracking().Where(x => !universityId.HasValue || x.UniversityId == universityId.Value)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(Guid? facultyId = null, CancellationToken ct = default) =>
        await _dbContext.Departments.AsNoTracking().Where(x => !facultyId.HasValue || x.FacultyId == facultyId.Value)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AcademicProgram>> GetProgramsAsync(Guid? departmentId = null, CancellationToken ct = default) =>
        await _dbContext.AcademicPrograms.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId.Value)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public Task<University?> GetUniversityByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Universities.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Campus?> GetCampusByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Campuses.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Faculty?> GetFacultyByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Faculties.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Department?> GetDepartmentByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AcademicProgram?> GetProgramByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(University entity, CancellationToken ct = default) =>
        _dbContext.Universities.AddAsync(entity, ct).AsTask();

    public Task AddAsync(Campus entity, CancellationToken ct = default) =>
        _dbContext.Campuses.AddAsync(entity, ct).AsTask();

    public Task AddAsync(Faculty entity, CancellationToken ct = default) =>
        _dbContext.Faculties.AddAsync(entity, ct).AsTask();

    public Task AddAsync(Department entity, CancellationToken ct = default) =>
        _dbContext.Departments.AddAsync(entity, ct).AsTask();

    public Task AddAsync(AcademicProgram entity, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AddAsync(entity, ct).AsTask();
}