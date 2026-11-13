using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Models;

public sealed record ConfigureAdmissionRequirementsRequest(IReadOnlyCollection<AdmissionDocumentType> RequiredDocumentTypes);

public sealed record AdmissionSubmissionReadinessResponse(
    Guid ApplicationId,
    bool RequirementsConfigured,
    bool CanSubmit,
    IReadOnlyCollection<AdmissionDocumentType> RequiredDocumentTypes,
    IReadOnlyCollection<AdmissionDocumentType> MissingVerifiedDocumentTypes);
