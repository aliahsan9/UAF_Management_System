using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Entities;

namespace UAF.Application.Features.Admissions;

public sealed class AdmissionApplicationService
{
    private readonly IAdmissionApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AdmissionApplicationService(IAdmissionApplicationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> CreateDraftAsync(CreateAdmissionApplicationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var number = NormalizeNumber(request.ApplicationNumber);
        if (await _repository.ExistsByApplicationNumberAsync(number, ct))
            throw new InvalidOperationException($"An admission application with number '{number}' already exists.");

        var application = new AdmissionApplication(number, request.ApplicantFullName, request.ApplicantEmail,
            request.AcademicProgramId, request.ApplicationDate, request.PhoneNumber);
        await _repository.AddAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return application.Id;
    }

    public async Task<ApplicantApplicationResponse> CreateApplicantDraftAsync(
        Guid ownerUserId, CreateApplicantDraftRequest request, CancellationToken ct = default)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Authenticated applicant is required.", nameof(ownerUserId));
        ArgumentNullException.ThrowIfNull(request);
        if (!await _repository.AcademicProgramIsActiveAsync(request.AcademicProgramId, ct))
            throw new KeyNotFoundException("The selected academic program is not available.");

        var number = $"UAF-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        var application = new AdmissionApplication(number, request.ApplicantFullName, request.ApplicantEmail,
            request.AcademicProgramId, DateOnly.FromDateTime(DateTime.UtcNow), request.PhoneNumber, ownerUserId);
        await _repository.AddAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(application);
    }

    public async Task<IReadOnlyList<ApplicantApplicationResponse>> GetMyApplicationsAsync(
        Guid ownerUserId, CancellationToken ct = default)
    {
        EnsureOwner(ownerUserId);
        var applications = await _repository.GetOwnedAsync(ownerUserId, ct);
        return applications.Select(Map).ToArray();
    }

    public async Task<ApplicantApplicationResponse> GetMyApplicationAsync(
        Guid ownerUserId, Guid applicationId, CancellationToken ct = default) =>
        Map(await GetOwnedRequiredAsync(ownerUserId, applicationId, ct));

    public async Task UpdateMyDraftAsync(Guid ownerUserId, Guid applicationId,
        UpdateApplicantDraftRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var application = await GetOwnedRequiredAsync(ownerUserId, applicationId, ct);
        application.UpdateApplicantDetails(request.ApplicantFullName, request.ApplicantEmail, request.PhoneNumber);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task SubmitMyApplicationAsync(Guid ownerUserId, Guid applicationId, CancellationToken ct = default) =>
        UpdateOwnedAsync(ownerUserId, applicationId, application => application.Submit(), ct);

    public Task WithdrawMyApplicationAsync(Guid ownerUserId, Guid applicationId, CancellationToken ct = default) =>
        UpdateOwnedAsync(ownerUserId, applicationId, application => application.Withdraw(), ct);

    public Task SubmitAsync(Guid id, CancellationToken ct = default) =>
        UpdateApplicationAsync(id, application => application.Submit(), ct);
    public Task WithdrawAsync(Guid id, CancellationToken ct = default) =>
        UpdateApplicationAsync(id, application => application.Withdraw(), ct);

    private async Task UpdateOwnedAsync(Guid ownerId, Guid id, Action<AdmissionApplication> update, CancellationToken ct)
    {
        var application = await GetOwnedRequiredAsync(ownerId, id, ct);
        update(application);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<AdmissionApplication> GetOwnedRequiredAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        EnsureOwner(ownerId);
        if (id == Guid.Empty) throw new ArgumentException("A valid application ID is required.", nameof(id));
        return await _repository.GetOwnedByIdAsync(id, ownerId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");
    }

    private async Task UpdateApplicationAsync(Guid id, Action<AdmissionApplication> update, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("A valid application ID is required.", nameof(id));
        var application = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Admission application '{id}' was not found.");
        update(application);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void EnsureOwner(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Authenticated applicant is required.", nameof(id));
    }

    private static string NormalizeNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Application number is required.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }

    private static ApplicantApplicationResponse Map(AdmissionApplication x) =>
        new(x.Id, x.ApplicationNumber, x.ApplicantFullName, x.ApplicantEmail, x.PhoneNumber,
            x.AcademicProgramId, x.ApplicationDate, x.Status, x.DecisionRemarks, x.CreatedAtUtc, x.UpdatedAtUtc);
}
