using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Abstractions;

public interface IAdmissionDocumentRepository
{
    Task<AdmissionApplication?> GetApplicationByIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid applicationId, Guid ownerUserId, CancellationToken ct = default);
    Task<AdmissionDocument?> GetOwnedDocumentAsync(Guid applicationId, Guid documentId, Guid ownerUserId, CancellationToken ct = default);
    Task<AdmissionDocument?> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionDocument>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionDocumentRequirement>> GetRequirementsForProgramAsync(Guid academicProgramId, CancellationToken ct = default);
    Task<bool> AcademicProgramExistsAsync(Guid academicProgramId, CancellationToken ct = default);
    Task<IReadOnlyCollection<AdmissionDocumentType>> GetVerifiedDocumentTypesAsync(Guid applicationId, CancellationToken ct = default);
    Task AddAsync(AdmissionDocument document, CancellationToken ct = default);
    Task AddRequirementAsync(AdmissionDocumentRequirement requirement, CancellationToken ct = default);
    void RemoveRequirement(AdmissionDocumentRequirement requirement);
}
