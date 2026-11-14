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

    public AdmissionApplicationServiceTests() => _service = new AdmissionApplicationService(_repository, _unitOfWork);

    [Fact]
    public async Task CreateDraftAsync_WithValidRequest_PersistsDraftAndReturnsId()
    {
        var id = await _service.CreateDraftAsync(CreateRequest());
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateDraftAsync(request));
        Assert.Contains("already exists", exception.Message);
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public void AdmissionApplication_CannotBeEditedAfterSubmission()
    {
        var application = new AdmissionApplication("UAF-2026-002", "Test Applicant",
            "applicant@example.com", Guid.NewGuid(), new DateOnly(2026, 11, 13));
        application.Submit();
        Assert.Throws<InvalidOperationException>(() =>
            application.UpdateApplicantDetails("Updated Applicant", "updated@example.com", null));
    }

    [Fact]
    public async Task ApplicantCanOnlyReadTheirOwnApplication()
    {
        var ownerId = Guid.NewGuid();
        var created = await _service.CreateApplicantDraftAsync(ownerId,
            new CreateApplicantDraftRequest("Applicant One", "one@example.com", Guid.NewGuid(), null));
        Assert.Equal(created.Id, (await _service.GetMyApplicationAsync(ownerId, created.Id)).Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetMyApplicationAsync(Guid.NewGuid(), created.Id));
    }

    [Fact]
    public async Task StaffQueue_IsPagedAndReturnsTotalCount()
    {
        await _service.CreateDraftAsync(CreateRequest());
        var page = await _service.GetStaffApplicationsAsync(new StaffAdmissionApplicationsQuery(Page: 1, PageSize: 1));
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task StaffDetail_ReturnsNotFoundForUnknownApplication()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetStaffApplicationAsync(Guid.NewGuid()));
    }

    private static CreateAdmissionApplicationRequest CreateRequest() =>
        new("UAF-2026-001", "Test Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), "03001234567");

    private sealed class FakeAdmissionApplicationRepository : IAdmissionApplicationRepository
    {
        private readonly Dictionary<Guid, AdmissionApplication> _applications = new();

        public Task<bool> ExistsByApplicationNumberAsync(string number, CancellationToken ct = default) =>
            Task.FromResult(_applications.Values.Any(x => string.Equals(x.ApplicationNumber, number, StringComparison.OrdinalIgnoreCase)));

        public Task<AdmissionApplication?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _applications.TryGetValue(id, out var application);
            return Task.FromResult(application);
        }

        public Task<AdmissionApplication?> GetOwnedByIdAsync(Guid id, Guid ownerId, CancellationToken ct = default)
        {
            _applications.TryGetValue(id, out var application);
            return Task.FromResult(application?.OwnerUserId == ownerId ? application : null);
        }

        public Task<IReadOnlyList<AdmissionApplication>> GetOwnedAsync(Guid ownerId, CancellationToken ct = default)
        {
            IReadOnlyList<AdmissionApplication> items = _applications.Values.Where(x => x.OwnerUserId == ownerId).ToArray();
            return Task.FromResult(items);
        }

        public Task<(IReadOnlyList<AdmissionApplication> Items, int TotalCount)> GetStaffPageAsync(
            AdmissionStatus? status, Guid? programId, string? search, int page, int pageSize, CancellationToken ct = default)
        {
            IEnumerable<AdmissionApplication> query = _applications.Values;
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            if (programId.HasValue) query = query.Where(x => x.AcademicProgramId == programId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.ApplicationNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.ApplicantFullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.ApplicantEmail.Contains(search, StringComparison.OrdinalIgnoreCase));
            var all = query.OrderByDescending(x => x.CreatedAtUtc).ToArray();
            return Task.FromResult(((IReadOnlyList<AdmissionApplication>)all.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), all.Length));
        }

        public Task<bool> AcademicProgramIsActiveAsync(Guid programId, CancellationToken ct = default) =>
            Task.FromResult(programId != Guid.Empty);

        public Task AddAsync(AdmissionApplication application, CancellationToken ct = default)
        {
            _applications.Add(application.Id, application);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
