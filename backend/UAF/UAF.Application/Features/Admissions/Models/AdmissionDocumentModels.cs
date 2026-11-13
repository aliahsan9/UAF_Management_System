using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Models;

public sealed record AdmissionDocumentResponse(
    Guid Id,
    Guid ApplicationId,
    AdmissionDocumentType DocumentType,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    AdmissionDocumentStatus Status,
    DateTimeOffset CreatedAtUtc,
    Guid? VerifiedByUserId,
    DateTimeOffset? VerifiedAtUtc,
    string? VerificationRemarks);

public sealed record VerifyAdmissionDocumentRequest(string? Remarks);
public sealed record RejectAdmissionDocumentRequest(string Remarks);
