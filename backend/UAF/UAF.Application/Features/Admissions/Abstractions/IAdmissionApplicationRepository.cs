using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Abstractions;

public interface IAdmissionApplicationRepository
{
    Task<bool> ExistsByApplicationNumberAsync(string applicationNumber, CancellationToken cancellationToken = default);
    Task<AdmissionApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdmissionApplication?> GetOwnedByIdAsync(Guid id, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionApplication>> GetOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AdmissionApplication> Items, int TotalCount)> GetStaffPageAsync(
        AdmissionStatus? status, Guid? academicProgramId, string? search, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> AcademicProgramIsActiveAsync(Guid academicProgramId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicProgram>> GetActiveAcademicProgramsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AdmissionApplication application, CancellationToken cancellationToken = default);
}
