using System.Security.Cryptography;
using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions;

public sealed class AdmissionDocumentService
{
    public const long MaximumFileSizeBytes = 10 * 1024 * 1024;
    private readonly IAdmissionDocumentRepository _repository;
    private readonly IAdmissionDocumentStorage _storage;
    private readonly IUnitOfWork _unitOfWork;

    public AdmissionDocumentService(
        IAdmissionDocumentRepository repository,
        IAdmissionDocumentStorage storage,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AdmissionDocumentResponse> UploadForApplicantAsync(
        Guid ownerUserId, Guid applicationId, AdmissionDocumentType documentType,
        string fileName, string contentType, Stream input, CancellationToken ct = default)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Authenticated applicant is required.", nameof(ownerUserId));
        if (!Enum.IsDefined(documentType)) throw new ArgumentOutOfRangeException(nameof(documentType));
        ArgumentNullException.ThrowIfNull(input);

        var application = await _repository.GetOwnedApplicationAsync(applicationId, ownerUserId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");
        if (application.Status != AdmissionStatus.Draft)
            throw new InvalidOperationException("Documents can only be uploaded while the application is a draft.");

        var safeName = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        var expectedType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new ArgumentException("Only PDF, JPEG, and PNG files are accepted.", nameof(fileName))
        };
        if (!string.Equals(contentType, expectedType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The declared content type does not match the file extension.", nameof(contentType));

        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(chunk, ct)) > 0)
        {
            total += read;
            if (total > MaximumFileSizeBytes)
                throw new ArgumentException("File size cannot exceed 10 MB.", nameof(input));
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        if (total == 0) throw new ArgumentException("Empty files are not accepted.", nameof(input));

        var bytes = buffer.ToArray();
        ValidateFileSignature(extension, bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        buffer.Position = 0;

        var storageKey = await _storage.SaveAsync(buffer, ct);
        try
        {
            var document = new AdmissionDocument(applicationId, ownerUserId, documentType,
                safeName, storageKey, expectedType, total, hash);
            await _repository.AddAsync(document, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Map(document);
        }
        catch
        {
            await _storage.DeleteIfExistsAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<AdmissionDocumentResponse>> GetMyDocumentsAsync(
        Guid ownerUserId, Guid applicationId, CancellationToken ct = default)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Authenticated applicant is required.", nameof(ownerUserId));
        if (await _repository.GetOwnedApplicationAsync(applicationId, ownerUserId, ct) is null)
            throw new KeyNotFoundException("Admission application was not found.");
        var documents = await _repository.GetForApplicationAsync(applicationId, ct);
        return documents.Select(Map).ToArray();
    }

    public async Task<(AdmissionDocumentResponse Document, Stream Content)> DownloadMyDocumentAsync(
        Guid ownerUserId, Guid applicationId, Guid documentId, CancellationToken ct = default)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Authenticated applicant is required.", nameof(ownerUserId));
        var document = await _repository.GetOwnedDocumentAsync(applicationId, documentId, ownerUserId, ct)
            ?? throw new KeyNotFoundException("Admission document was not found.");
        return (Map(document), await _storage.OpenReadAsync(document.StorageKey, ct));
    }

    public async Task<IReadOnlyList<AdmissionDocumentResponse>> GetForStaffAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("A valid application ID is required.", nameof(applicationId));
        var documents = await _repository.GetForApplicationAsync(applicationId, ct);
        return documents.Select(Map).ToArray();
    }

    public async Task<(AdmissionDocumentResponse Document, Stream Content)> DownloadForStaffAsync(
        Guid documentId, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Admission document was not found.");
        return (Map(document), await _storage.OpenReadAsync(document.StorageKey, ct));
    }

    public Task VerifyAsync(Guid documentId, Guid reviewerUserId, string? remarks, CancellationToken ct = default) =>
        ReviewAsync(documentId, document => document.Verify(reviewerUserId, DateTimeOffset.UtcNow, remarks), ct);

    public Task RejectAsync(Guid documentId, Guid reviewerUserId, string remarks, CancellationToken ct = default) =>
        ReviewAsync(documentId, document => document.Reject(reviewerUserId, DateTimeOffset.UtcNow, remarks), ct);

    private async Task ReviewAsync(Guid documentId, Action<AdmissionDocument> review, CancellationToken ct)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("A valid document ID is required.", nameof(documentId));
        var document = await _repository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Admission document was not found.");
        review(document);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void ValidateFileSignature(string extension, byte[] bytes)
    {
        var valid = extension switch
        {
            ".pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            _ => false
        };
        if (!valid) throw new ArgumentException("The file content does not match an accepted document format.", nameof(bytes));
    }

    private static AdmissionDocumentResponse Map(AdmissionDocument x) =>
        new(x.Id, x.ApplicationId, x.DocumentType, x.OriginalFileName, x.ContentType,
            x.SizeBytes, x.Status, x.CreatedAtUtc, x.VerifiedByUserId, x.VerifiedAtUtc, x.VerificationRemarks);
}
