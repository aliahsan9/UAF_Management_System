using UAF.Domain.Entities;

namespace UAF.Application.Features.Admissions.Abstractions;

/// <summary>
/// Persistence operations required by admission application use cases.
/// Implemented by the Infrastructure layer.
/// </summary>
public interface IAdmissionApplicationRepository
{
    Task<bool> ExistsByApplicationNumberAsync(
        string applicationNumber,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken = default);
}
