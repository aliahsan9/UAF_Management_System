using UAF.Domain.Entities;

namespace UAF.Application.Features.Admissions.Abstractions;

public interface IAdmissionDocumentRepository
{
    Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid applicationId, Guid ownerUserId, CancellationToken ct = default);
    Task<AdmissionDocument?> GetOwnedDocumentAsync(Guid applicationId, Guid documentId, Guid ownerUserId, CancellationToken ct = default);
    Task<AdmissionDocument?> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionDocument>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task AddAsync(AdmissionDocument document, CancellationToken ct = default);
}
