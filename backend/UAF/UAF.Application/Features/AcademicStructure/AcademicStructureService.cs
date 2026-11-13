using UAF.Application.Common.Abstractions;
using UAF.Application.Features.AcademicStructure.Abstractions;
using UAF.Application.Features.AcademicStructure.Models;
using UAF.Domain.Entities;

namespace UAF.Application.Features.AcademicStructure;

/// <summary>
/// Coordinates academic hierarchy use cases. Domain entities enforce their own invariants;
/// this service validates relationships and uniqueness before persistence.
/// </summary>
public sealed class AcademicStructureService
{
    private readonly IAcademicStructureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AcademicStructureService(
        IAcademicStructureRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<UniversityResponse>> GetUniversitiesAsync(CancellationToken ct = default) =>
        (await _repository.GetUniversitiesAsync(ct))
            .Select(x => new UniversityResponse(x.Id, x.Name, x.ShortName)).ToArray();

    public async Task<IReadOnlyList<CampusResponse>> GetCampusesAsync(Guid? universityId = null, CancellationToken ct = default) =>
        (await _repository.GetCampusesAsync(universityId, ct))
            .Select(x => new CampusResponse(x.Id, x.UniversityId, x.Name, x.Code, x.Address, x.IsActive)).ToArray();

    public async Task<IReadOnlyList<FacultyResponse>> GetFacultiesAsync(Guid? universityId = null, CancellationToken ct = default) =>
        (await _repository.GetFacultiesAsync(universityId, ct))
            .Select(x => new FacultyResponse(x.Id, x.UniversityId, x.Name, x.Code, x.IsActive)).ToArray();

    public async Task<IReadOnlyList<DepartmentResponse>> GetDepartmentsAsync(Guid? facultyId = null, CancellationToken ct = default) =>
        (await _repository.GetDepartmentsAsync(facultyId, ct))
            .Select(x => new DepartmentResponse(x.Id, x.FacultyId, x.Name, x.Code, x.IsActive)).ToArray();

    public async Task<IReadOnlyList<AcademicProgramResponse>> GetProgramsAsync(Guid? departmentId = null, CancellationToken ct = default) =>
        (await _repository.GetProgramsAsync(departmentId, ct))
            .Select(x => new AcademicProgramResponse(x.Id, x.DepartmentId, x.Name, x.Code, x.DegreeLevel, x.DurationInYears, x.IsActive)).ToArray();

    public async Task<Guid> CreateUniversityAsync(CreateUniversityRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new University(request.Name, request.ShortName);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateUniversityAsync(Guid id, UpdateUniversityRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await GetRequiredAsync(_repository.GetUniversityByIdAsync(id, ct), id, "University");
        entity.UpdateDetails(request.Name, request.ShortName);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreateCampusAsync(CreateCampusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureId(request.UniversityId, nameof(request.UniversityId));
        await EnsureExistsAsync(_repository.UniversityExistsAsync(request.UniversityId, ct), "University", request.UniversityId);
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsCampusCodeInUseAsync(request.UniversityId, code, cancellationToken: ct), "Campus", code);
        var entity = new Campus(request.UniversityId, request.Name, code, request.Address);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateCampusAsync(Guid id, UpdateCampusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await GetRequiredAsync(_repository.GetCampusByIdAsync(id, ct), id, "Campus");
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsCampusCodeInUseAsync(entity.UniversityId, code, id, ct), "Campus", code);
        entity.UpdateDetails(request.Name, code, request.Address);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task DeactivateCampusAsync(Guid id, CancellationToken ct = default) =>
        DeactivateAsync(_repository.GetCampusByIdAsync(id, ct), id, "Campus", x => x.Deactivate(), ct);

    public async Task<Guid> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureId(request.UniversityId, nameof(request.UniversityId));
        await EnsureExistsAsync(_repository.UniversityExistsAsync(request.UniversityId, ct), "University", request.UniversityId);
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsFacultyCodeInUseAsync(request.UniversityId, code, cancellationToken: ct), "Faculty", code);
        var entity = new Faculty(request.UniversityId, request.Name, code);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateFacultyAsync(Guid id, UpdateFacultyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await GetRequiredAsync(_repository.GetFacultyByIdAsync(id, ct), id, "Faculty");
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsFacultyCodeInUseAsync(entity.UniversityId, code, id, ct), "Faculty", code);
        entity.UpdateDetails(request.Name, code);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task DeactivateFacultyAsync(Guid id, CancellationToken ct = default) =>
        DeactivateAsync(_repository.GetFacultyByIdAsync(id, ct), id, "Faculty", x => x.Deactivate(), ct);

    public async Task<Guid> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureId(request.FacultyId, nameof(request.FacultyId));
        await EnsureExistsAsync(_repository.FacultyExistsAsync(request.FacultyId, ct), "Faculty", request.FacultyId);
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsDepartmentCodeInUseAsync(request.FacultyId, code, cancellationToken: ct), "Department", code);
        var entity = new Department(request.FacultyId, request.Name, code);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await GetRequiredAsync(_repository.GetDepartmentByIdAsync(id, ct), id, "Department");
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsDepartmentCodeInUseAsync(entity.FacultyId, code, id, ct), "Department", code);
        entity.UpdateDetails(request.Name, code);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task DeactivateDepartmentAsync(Guid id, CancellationToken ct = default) =>
        DeactivateAsync(_repository.GetDepartmentByIdAsync(id, ct), id, "Department", x => x.Deactivate(), ct);

    public async Task<Guid> CreateProgramAsync(CreateAcademicProgramRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureId(request.DepartmentId, nameof(request.DepartmentId));
        await EnsureExistsAsync(_repository.DepartmentExistsAsync(request.DepartmentId, ct), "Department", request.DepartmentId);
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsProgramCodeInUseAsync(request.DepartmentId, code, cancellationToken: ct), "Academic program", code);
        var entity = new AcademicProgram(request.DepartmentId, request.Name, code, request.DegreeLevel, request.DurationInYears);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateProgramAsync(Guid id, UpdateAcademicProgramRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await GetRequiredAsync(_repository.GetProgramByIdAsync(id, ct), id, "Academic program");
        var code = NormalizeCode(request.Code);
        await EnsureUniqueAsync(_repository.IsProgramCodeInUseAsync(entity.DepartmentId, code, id, ct), "Academic program", code);
        entity.UpdateDetails(request.Name, code, request.DegreeLevel, request.DurationInYears);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task DeactivateProgramAsync(Guid id, CancellationToken ct = default) =>
        DeactivateAsync(_repository.GetProgramByIdAsync(id, ct), id, "Academic program", x => x.Deactivate(), ct);

    private async Task DeactivateAsync<T>(
        Task<T?> entityTask, Guid id, string resourceName, Action<T> deactivate, CancellationToken ct)
        where T : class
    {
        var entity = await GetRequiredAsync(entityTask, id, resourceName);
        deactivate(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static async Task<T> GetRequiredAsync<T>(Task<T?> task, Guid id, string name) where T : class
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"A valid {name} ID is required.", nameof(id));
        return await task ?? throw new KeyNotFoundException($"{name} '{id}' was not found.");
    }

    private static async Task EnsureExistsAsync(Task<bool> existsTask, string name, Guid id)
    {
        if (!await existsTask)
            throw new KeyNotFoundException($"{name} '{id}' was not found.");
    }

    private static async Task EnsureUniqueAsync(Task<bool> existsTask, string name, string code)
    {
        if (await existsTask)
            throw new InvalidOperationException($"{name} code '{code}' is already in use within its parent.");
    }

    private static void EnsureId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid parent ID is required.", parameterName);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        return code.Trim().ToUpperInvariant();
    }
}