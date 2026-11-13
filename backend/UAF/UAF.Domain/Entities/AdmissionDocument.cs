using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>Private file metadata for a document attached to an admission application.</summary>
public sealed class AdmissionDocument : AuditableEntity
{
    private AdmissionDocument()
    {
        OriginalFileName = string.Empty;
        StorageKey = string.Empty;
        ContentType = string.Empty;
        Sha256 = string.Empty;
    }

    public AdmissionDocument(
        Guid applicationId,
        Guid uploadedByUserId,
        AdmissionDocumentType documentType,
        string originalFileName,
        string storageKey,
        string contentType,
        long sizeBytes,
        string sha256)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("Application is required.", nameof(applicationId));
        if (uploadedByUserId == Guid.Empty) throw new ArgumentException("Uploader is required.", nameof(uploadedByUserId));
        if (!Enum.IsDefined(documentType)) throw new ArgumentOutOfRangeException(nameof(documentType));
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Trim().Length > 255)
            throw new ArgumentException("A file name up to 255 characters is required.", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Length > 200)
            throw new ArgumentException("A valid storage key is required.", nameof(storageKey));
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 100)
            throw new ArgumentException("A valid content type is required.", nameof(contentType));
        if (sizeBytes <= 0 || sizeBytes > 10 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be between 1 byte and 10 MB.");
        if (string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64)
            throw new ArgumentException("A SHA-256 hash is required.", nameof(sha256));

        ApplicationId = applicationId;
        UploadedByUserId = uploadedByUserId;
        DocumentType = documentType;
        OriginalFileName = Path.GetFileName(originalFileName.Trim());
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256.ToLowerInvariant();
        Status = AdmissionDocumentStatus.PendingVerification;
    }

    public Guid ApplicationId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public AdmissionDocumentType DocumentType { get; private set; }
    public string OriginalFileName { get; private set; }
    public string StorageKey { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; }
    public AdmissionDocumentStatus Status { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public string? VerificationRemarks { get; private set; }

    public void Verify(Guid reviewerUserId, DateTimeOffset nowUtc, string? remarks = null)
    {
        EnsurePending();
        if (reviewerUserId == Guid.Empty) throw new ArgumentException("Reviewer is required.", nameof(reviewerUserId));
        Status = AdmissionDocumentStatus.Verified;
        VerifiedByUserId = reviewerUserId;
        VerifiedAtUtc = nowUtc;
        VerificationRemarks = NormalizeRemarks(remarks);
        MarkUpdated();
    }

    public void Reject(Guid reviewerUserId, DateTimeOffset nowUtc, string remarks)
    {
        EnsurePending();
        if (reviewerUserId == Guid.Empty) throw new ArgumentException("Reviewer is required.", nameof(reviewerUserId));
        if (string.IsNullOrWhiteSpace(remarks)) throw new ArgumentException("A rejection reason is required.", nameof(remarks));
        Status = AdmissionDocumentStatus.Rejected;
        VerifiedByUserId = reviewerUserId;
        VerifiedAtUtc = nowUtc;
        VerificationRemarks = NormalizeRemarks(remarks);
        MarkUpdated();
    }

    private void EnsurePending()
    {
        if (Status != AdmissionDocumentStatus.PendingVerification)
            throw new InvalidOperationException("Only pending documents can be reviewed.");
    }

    private static string? NormalizeRemarks(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 2000) throw new ArgumentException("Remarks cannot exceed 2000 characters.", nameof(value));
        return normalized;
    }
}
