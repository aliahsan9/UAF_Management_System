using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Admissions;

public sealed class AdmissionApplicationServiceTests
{
    private readonly FakeAdmissionApplicationRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AdmissionApplicationService _service;

    public AdmissionApplicationServiceTests()
    {
        _service = new AdmissionApplicationService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task CreateDraftAsync_WithValidRequest_PersistsDraftAndReturnsId()
    {
        var request = CreateRequest();

        var id = await _service.CreateDraftAsync(request);

        var saved = await _repository.GetByIdAsync(id);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(saved);
        Assert.Equal("UAF-2026-001", saved.ApplicationNumber);
        Assert.Equal(AdmissionStatus.Draft, saved.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenApplicationNumberAlreadyExists_ThrowsAndDoesNotSave()
    {
        var request = CreateRequest();
        await _service.CreateDraftAsync(request);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateDraftAsync(request));

        Assert.Contains("already exists", exception.Message);
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task SubmitAsync_WhenApplicationIsDraft_ChangesStatusAndSaves()
    {
        var id = await _service.CreateDraftAsync(CreateRequest());

        await _service.SubmitAsync(id);

        var saved = await _repository.GetByIdAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(AdmissionStatus.Submitted, saved.Status);
        Assert.Equal(2, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task AdmissionWorkflow_CanMoveFromDraftToAccepted()
    {
        var id = await _service.CreateDraftAsync(CreateRequest());

        await _service.SubmitAsync(id);
        await _service.StartReviewAsync(id);
        await _service.AcceptAsync(id, "Eligibility verified.");

        var saved = await _repository.GetByIdAsync(id);

        Assert.NotNull(saved);
        Assert.Equal(AdmissionStatus.Accepted, saved.Status);
        Assert.Equal("Eligibility verified.", saved.DecisionRemarks);
        Assert.Equal(4, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task RejectAsync_WhenApplicationIsNotUnderReview_Throws()
    {
        var id = await _service.CreateDraftAsync(CreateRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectAsync(id, "Missing documents."));

        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task SubmitAsync_WhenApplicationDoesNotExist_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SubmitAsync(Guid.NewGuid()));

        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task SubmitAsync_WithEmptyId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SubmitAsync(Guid.Empty));

        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public void AdmissionApplication_CannotBeEditedAfterSubmission()
    {
        var application = new AdmissionApplication(
            "UAF-2026-002",
            "Test Applicant",
            "applicant@example.com",
            Guid.NewGuid(),
            new DateOnly(2026, 11, 13));

        application.Submit();

        Assert.Throws<InvalidOperationException>(() =>
            application.UpdateApplicantDetails(
                "Updated Applicant",
                "updated@example.com",
                null));
    }

    private static CreateAdmissionApplicationRequest CreateRequest() =>
        new(
            "UAF-2026-001",
            "Test Applicant",
            "applicant@example.com",
            Guid.NewGuid(),
            new DateOnly(2026, 11, 13),
            "03001234567");

    private sealed class FakeAdmissionApplicationRepository : IAdmissionApplicationRepository
    {
        private readonly Dictionary<Guid, AdmissionApplication> _applications = new();

        public Task<bool> ExistsByApplicationNumberAsync(
            string applicationNumber,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exists = _applications.Values.Any(application =>
                string.Equals(
                    application.ApplicationNumber,
                    applicationNumber,
                    StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task<AdmissionApplication?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _applications.TryGetValue(id, out var application);
            return Task.FromResult(application);
        }

        public Task AddAsync(
            AdmissionApplication application,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _applications.Add(application.Id, application);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
