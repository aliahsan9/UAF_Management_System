using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Admissions;

public sealed class AdmissionDocumentServiceTests
{
    [Fact]
    public async Task ApplicantCanUploadValidPdfToOwnedDraft()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-001", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var repository = new FakeRepository(app);
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork();
        var service = new AdmissionDocumentService(repository, storage, unitOfWork);

        var response = await service.UploadForApplicantAsync(ownerId, app.Id,
            AdmissionDocumentType.AcademicTranscript, "transcript.pdf", "application/pdf",
            new MemoryStream("%PDF-1.7 test document"u8.ToArray()));

        Assert.Equal(AdmissionDocumentStatus.PendingVerification, response.Status);
        Assert.Equal("transcript.pdf", response.OriginalFileName);
        Assert.Equal(1, storage.SavedFiles);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task ApplicantCannotUploadToAnotherApplicantsApplication()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-002", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: Guid.NewGuid());
        var service = new AdmissionDocumentService(new FakeRepository(app), new FakeStorage(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UploadForApplicantAsync(ownerId, app.Id, AdmissionDocumentType.IdentityDocument,
                "identity.pdf", "application/pdf", new MemoryStream("%PDF-1.7"u8.ToArray())));
    }

    [Fact]
    public async Task ApplicantCannotUploadAfterApplicationIsSubmitted()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-006", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        app.Submit();
        var storage = new FakeStorage();
        var service = new AdmissionDocumentService(new FakeRepository(app), storage, new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadForApplicantAsync(ownerId, app.Id, AdmissionDocumentType.IdentityDocument,
                "identity.pdf", "application/pdf", new MemoryStream("%PDF-1.7"u8.ToArray())));

        Assert.Equal(0, storage.SavedFiles);
    }

    [Fact]
    public async Task UploadRejectsFilesLargerThanTenMegabytes()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-007", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var storage = new FakeStorage();
        var bytes = new byte[checked((int)AdmissionDocumentService.MaximumFileSizeBytes + 1)];
        "%PDF-"u8.CopyTo(bytes);
        var service = new AdmissionDocumentService(new FakeRepository(app), storage, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadForApplicantAsync(ownerId, app.Id, AdmissionDocumentType.AcademicTranscript,
                "transcript.pdf", "application/pdf", new MemoryStream(bytes)));

        Assert.Equal(0, storage.SavedFiles);
    }

    [Fact]
    public async Task UploadRejectsFileWithMismatchedSignature()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-003", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var storage = new FakeStorage();
        var service = new AdmissionDocumentService(new FakeRepository(app), storage, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadForApplicantAsync(ownerId, app.Id, AdmissionDocumentType.AcademicTranscript,
                "transcript.pdf", "application/pdf", new MemoryStream("not a pdf"u8.ToArray())));

        Assert.Equal(0, storage.SavedFiles);
    }

    [Fact]
    public async Task VerificationRecordsReviewerAndPreventsSecondReview()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-004", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var repository = new FakeRepository(app);
        var service = new AdmissionDocumentService(repository, new FakeStorage(), new FakeUnitOfWork());
        var upload = await service.UploadForApplicantAsync(ownerId, app.Id,
            AdmissionDocumentType.IdentityDocument, "id.pdf", "application/pdf",
            new MemoryStream("%PDF-1.7 identity"u8.ToArray()));
        var reviewerId = Guid.NewGuid();

        await service.VerifyAsync(upload.Id, reviewerId, "Name and details are legible.");

        var saved = await repository.GetByIdAsync(upload.Id);
        Assert.NotNull(saved);
        Assert.Equal(AdmissionDocumentStatus.Verified, saved.Status);
        Assert.Equal(reviewerId, saved.VerifiedByUserId);
        Assert.NotNull(saved.VerifiedAtUtc);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RejectAsync(upload.Id, reviewerId, "Already reviewed."));
    }

    [Fact]
    public async Task RejectionRequiresReason()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-005", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var repository = new FakeRepository(app);
        var service = new AdmissionDocumentService(repository, new FakeStorage(), new FakeUnitOfWork());
        var upload = await service.UploadForApplicantAsync(ownerId, app.Id,
            AdmissionDocumentType.Photograph, "photo.jpg", "image/jpeg",
            new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RejectAsync(upload.Id, Guid.NewGuid(), " "));
    }

    [Fact]
    public async Task SubmissionIsBlockedUntilEveryRequiredDocumentIsVerified()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-008", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var repository = new FakeRepository(app);
        var unitOfWork = new FakeUnitOfWork();
        var service = new AdmissionDocumentService(repository, new FakeStorage(), unitOfWork);
        var staffId = Guid.NewGuid();

        await service.ConfigureRequirementsAsync(app.AcademicProgramId, staffId,
            new[] { AdmissionDocumentType.IdentityDocument, AdmissionDocumentType.AcademicTranscript });

        var readiness = await service.GetSubmissionReadinessAsync(ownerId, app.Id);
        Assert.False(readiness.CanSubmit);
        Assert.Equal(2, readiness.MissingVerifiedDocumentTypes.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitMyApplicationAsync(ownerId, app.Id));

        var upload = await service.UploadForApplicantAsync(ownerId, app.Id,
            AdmissionDocumentType.IdentityDocument, "identity.pdf", "application/pdf",
            new MemoryStream("%PDF-1.7 identity"u8.ToArray()));
        await service.VerifyAsync(upload.Id, staffId, null);

        readiness = await service.GetSubmissionReadinessAsync(ownerId, app.Id);
        Assert.False(readiness.CanSubmit);
        Assert.Contains(AdmissionDocumentType.AcademicTranscript, readiness.MissingVerifiedDocumentTypes);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitMyApplicationAsync(ownerId, app.Id));
    }

    [Fact]
    public async Task SubmissionSucceedsWhenAllConfiguredDocumentsAreVerified()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-009", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var repository = new FakeRepository(app);
        var service = new AdmissionDocumentService(repository, new FakeStorage(), new FakeUnitOfWork());
        var staffId = Guid.NewGuid();

        await service.ConfigureRequirementsAsync(app.AcademicProgramId, staffId,
            new[] { AdmissionDocumentType.AcademicTranscript });
        var upload = await service.UploadForApplicantAsync(ownerId, app.Id,
            AdmissionDocumentType.AcademicTranscript, "transcript.pdf", "application/pdf",
            new MemoryStream("%PDF-1.7 transcript"u8.ToArray()));
        await service.VerifyAsync(upload.Id, staffId, "Verified");

        var readiness = await service.GetSubmissionReadinessAsync(ownerId, app.Id);
        Assert.True(readiness.CanSubmit);
        await service.SubmitMyApplicationAsync(ownerId, app.Id);
        Assert.Equal(AdmissionStatus.Submitted, app.Status);
    }

    [Fact]
    public async Task SubmissionIsBlockedWhenProgramHasNoConfiguredChecklist()
    {
        var ownerId = Guid.NewGuid();
        var app = new AdmissionApplication("UAF-TEST-010", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), ownerUserId: ownerId);
        var service = new AdmissionDocumentService(new FakeRepository(app), new FakeStorage(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitMyApplicationAsync(ownerId, app.Id));
    }

    private sealed class FakeRepository(AdmissionApplication application) : IAdmissionDocumentRepository
    {
        private readonly Dictionary<Guid, AdmissionDocument> _documents = new();
        private readonly List<AdmissionDocumentRequirement> _requirements = new();
        public Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid applicationId, Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(application.Id == applicationId && application.OwnerUserId == ownerUserId ? application : null);
        public Task<AdmissionDocument?> GetOwnedDocumentAsync(Guid applicationId, Guid documentId, Guid ownerUserId, CancellationToken ct = default)
        {
            _documents.TryGetValue(documentId, out var document);
            return Task.FromResult(document is not null && document.ApplicationId == applicationId &&
                application.OwnerUserId == ownerUserId ? document : null);
        }
        public Task<AdmissionDocument?> GetByIdAsync(Guid documentId, CancellationToken ct = default)
        {
            _documents.TryGetValue(documentId, out var document);
            return Task.FromResult(document);
        }
        public Task<IReadOnlyList<AdmissionDocument>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default)
        {
            IReadOnlyList<AdmissionDocument> documents = _documents.Values.Where(x => x.ApplicationId == applicationId).ToArray();
            return Task.FromResult(documents);
        }
        public Task<IReadOnlyList<AdmissionDocumentRequirement>> GetRequirementsForProgramAsync(Guid academicProgramId, CancellationToken ct = default)
        {
            IReadOnlyList<AdmissionDocumentRequirement> items = _requirements.Where(x => x.AcademicProgramId == academicProgramId).ToArray();
            return Task.FromResult(items);
        }
        public Task<bool> AcademicProgramExistsAsync(Guid academicProgramId, CancellationToken ct = default) =>
            Task.FromResult(application.AcademicProgramId == academicProgramId);
        public Task<IReadOnlyCollection<AdmissionDocumentType>> GetVerifiedDocumentTypesAsync(Guid applicationId, CancellationToken ct = default)
        {
            IReadOnlyCollection<AdmissionDocumentType> types = _documents.Values
                .Where(x => x.ApplicationId == applicationId && x.Status == AdmissionDocumentStatus.Verified)
                .Select(x => x.DocumentType).Distinct().ToArray();
            return Task.FromResult(types);
        }
        public Task AddRequirementAsync(AdmissionDocumentRequirement requirement, CancellationToken ct = default)
        {
            _requirements.Add(requirement);
            return Task.CompletedTask;
        }
        public void RemoveRequirement(AdmissionDocumentRequirement requirement) => _requirements.Remove(requirement);
        public Task AddAsync(AdmissionDocument document, CancellationToken ct = default)
        {
            _documents.Add(document.Id, document);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorage : IAdmissionDocumentStorage
    {
        public int SavedFiles { get; private set; }
        private readonly Dictionary<string, byte[]> _files = new();
        public async Task<string> SaveAsync(Stream content, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            var key = $"{Guid.NewGuid():N}.bin";
            _files[key] = buffer.ToArray();
            SavedFiles++;
            return key;
        }
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream(_files[storageKey], writable: false));
        public Task DeleteIfExistsAsync(string storageKey, CancellationToken ct = default)
        {
            _files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
