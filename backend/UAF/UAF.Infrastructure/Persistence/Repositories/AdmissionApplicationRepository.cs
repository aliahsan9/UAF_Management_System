using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of admission application persistence operations.
/// </summary>
internal sealed class AdmissionApplicationRepository : IAdmissionApplicationRepository
{
    private readonly UafDbContext _dbContext;

    public AdmissionApplicationRepository(UafDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByApplicationNumberAsync(
        string applicationNumber,
        CancellationToken cancellationToken = default) =>
        _dbContext.AdmissionApplications
            .AnyAsync(
                application => application.ApplicationNumber == applicationNumber,
                cancellationToken);

    public Task<AdmissionApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _dbContext.AdmissionApplications
            .FirstOrDefaultAsync(application => application.Id == id, cancellationToken);

    public async Task AddAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);

        await _dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }
}
