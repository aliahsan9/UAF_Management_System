using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;

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

    public Task<bool> AcademicProgramIsActiveAsync(Guid programId, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AnyAsync(x => x.Id == programId && x.IsActive, ct);

    public async Task AddAsync(AdmissionApplication application, CancellationToken ct = default) =>
        await _dbContext.AdmissionApplications.AddAsync(application, ct);
}
