using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class AdmissionDocumentRepository : IAdmissionDocumentRepository
{
    private readonly UafDbContext _db;
    public AdmissionDocumentRepository(UafDbContext db) => _db = db;

    public Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid applicationId, Guid ownerUserId, CancellationToken ct = default) =>
        _db.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == applicationId && x.OwnerUserId == ownerUserId, ct);

    public Task<AdmissionDocument?> GetOwnedDocumentAsync(Guid applicationId, Guid documentId, Guid ownerUserId, CancellationToken ct = default) =>
        _db.AdmissionDocuments.FirstOrDefaultAsync(x =>
            x.Id == documentId && x.ApplicationId == applicationId &&
            _db.AdmissionApplications.Any(a => a.Id == x.ApplicationId && a.OwnerUserId == ownerUserId), ct);

    public Task<AdmissionDocument?> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
        _db.AdmissionDocuments.FirstOrDefaultAsync(x => x.Id == documentId, ct);

    public async Task<IReadOnlyList<AdmissionDocument>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default) =>
        await _db.AdmissionDocuments.AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AdmissionDocumentRequirement>> GetRequirementsForProgramAsync(Guid academicProgramId, CancellationToken ct = default) =>
        await _db.AdmissionDocumentRequirements
            .Where(x => x.AcademicProgramId == academicProgramId && x.IsRequired)
            .OrderBy(x => x.DocumentType)
            .ToListAsync(ct);

    public Task<bool> AcademicProgramExistsAsync(Guid academicProgramId, CancellationToken ct = default) =>
        _db.AcademicPrograms.AnyAsync(x => x.Id == academicProgramId, ct);

    public async Task<IReadOnlyCollection<AdmissionDocumentType>> GetVerifiedDocumentTypesAsync(Guid applicationId, CancellationToken ct = default) =>
        await _db.AdmissionDocuments.AsNoTracking()
            .Where(x => x.ApplicationId == applicationId && x.Status == AdmissionDocumentStatus.Verified)
            .Select(x => x.DocumentType)
            .Distinct()
            .ToArrayAsync(ct);

    public async Task AddAsync(AdmissionDocument document, CancellationToken ct = default) =>
        await _db.AdmissionDocuments.AddAsync(document, ct);

    public async Task AddRequirementAsync(AdmissionDocumentRequirement requirement, CancellationToken ct = default) =>
        await _db.AdmissionDocumentRequirements.AddAsync(requirement, ct);

    public void RemoveRequirement(AdmissionDocumentRequirement requirement) =>
        _db.AdmissionDocumentRequirements.Remove(requirement);
}
