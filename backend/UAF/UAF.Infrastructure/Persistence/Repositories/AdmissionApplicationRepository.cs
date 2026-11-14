using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class AdmissionApplicationRepository : IAdmissionApplicationRepository
{
    private readonly UafDbContext _dbContext;
    public AdmissionApplicationRepository(UafDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> ExistsByApplicationNumberAsync(string number, CancellationToken ct = default) =>
        _dbContext.AdmissionApplications.AnyAsync(x => x.ApplicationNumber == number, ct);

    public Task<AdmissionApplication?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AdmissionApplication?> GetOwnedByIdAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _dbContext.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == ownerId, ct);

    public async Task<IReadOnlyList<AdmissionApplication>> GetOwnedAsync(Guid ownerId, CancellationToken ct = default) =>
        await _dbContext.AdmissionApplications.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<AdmissionApplication> Items, int TotalCount)> GetStaffPageAsync(
        AdmissionStatus? status, Guid? academicProgramId, string? search, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.AdmissionApplications.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (academicProgramId.HasValue && academicProgramId.Value != Guid.Empty)
            query = query.Where(x => x.AcademicProgramId == academicProgramId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ApplicationNumber.Contains(term) ||
                x.ApplicantFullName.Contains(term) || x.ApplicantEmail.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        return (items, totalCount);
    }

    public Task<bool> AcademicProgramIsActiveAsync(Guid programId, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AnyAsync(x => x.Id == programId && x.IsActive, ct);

    public async Task<IReadOnlyList<AcademicProgram>> GetActiveAcademicProgramsAsync(CancellationToken ct = default) =>
        await _dbContext.AcademicPrograms.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ThenBy(x => x.Code).ToArrayAsync(ct);

    public async Task AddAsync(AdmissionApplication application, CancellationToken ct = default) =>
        await _dbContext.AdmissionApplications.AddAsync(application, ct);
}
