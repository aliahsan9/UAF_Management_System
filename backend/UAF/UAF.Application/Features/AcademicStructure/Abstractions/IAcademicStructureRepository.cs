using UAF.Domain.Entities;

namespace UAF.Application.Features.AcademicStructure.Abstractions;

/// <summary>
/// Persistence operations for the university's academic hierarchy.
/// </summary>
public interface IAcademicStructureRepository
{
    Task<bool> UniversityExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> FacultyExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DepartmentExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsCampusCodeInUseAsync(Guid universityId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsFacultyCodeInUseAsync(Guid universityId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsDepartmentCodeInUseAsync(Guid facultyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsProgramCodeInUseAsync(Guid departmentId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<University>> GetUniversitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campus>> GetCampusesAsync(Guid? universityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Faculty>> GetFacultiesAsync(Guid? universityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(Guid? facultyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicProgram>> GetProgramsAsync(Guid? departmentId = null, CancellationToken cancellationToken = default);

    Task<University?> GetUniversityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Campus?> GetCampusByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Faculty?> GetFacultyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Department?> GetDepartmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AcademicProgram?> GetProgramByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(University entity, CancellationToken cancellationToken = default);
    Task AddAsync(Campus entity, CancellationToken cancellationToken = default);
    Task AddAsync(Faculty entity, CancellationToken cancellationToken = default);
    Task AddAsync(Department entity, CancellationToken cancellationToken = default);
    Task AddAsync(AcademicProgram entity, CancellationToken cancellationToken = default);
}