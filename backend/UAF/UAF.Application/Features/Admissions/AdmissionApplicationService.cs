using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

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
        return MapApplicant(application);
    }

    public async Task<IReadOnlyList<ApplicantAcademicProgramResponse>> GetApplicantProgramsAsync(CancellationToken ct = default)\n    {\n        var programs = await _repository.GetActiveAcademicProgramsAsync(ct);\n        return programs.Select(x => new ApplicantAcademicProgramResponse(x.Id, x.Name, x.Code, x.DegreeLevel, x.DurationInYears)).ToArray();\n    }\n\n    public async Task<IReadOnlyList<ApplicantApplicationResponse>> GetMyApplicationsAsync(
        Guid ownerUserId, CancellationToken ct = default)
    {
        EnsureOwner(ownerUserId);
        var applications = await _repository.GetOwnedAsync(ownerUserId, ct);
        return applications.Select(MapApplicant).ToArray();
    }

    public async Task<ApplicantApplicationResponse> GetMyApplicationAsync(
        Guid ownerUserId, Guid applicationId, CancellationToken ct = default) =>
        MapApplicant(await GetOwnedRequiredAsync(ownerUserId, applicationId, ct));

    public async Task UpdateMyDraftAsync(Guid ownerUserId, Guid applicationId,
        UpdateApplicantDraftRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var application = await GetOwnedRequiredAsync(ownerUserId, applicationId, ct);
        application.UpdateApplicantDetails(request.ApplicantFullName, request.ApplicantEmail, request.PhoneNumber);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task WithdrawMyApplicationAsync(Guid ownerUserId, Guid applicationId, CancellationToken ct = default) =>
        UpdateOwnedAsync(ownerUserId, applicationId, application => application.Withdraw(), ct);

    public Task WithdrawAsync(Guid id, CancellationToken ct = default) =>
        UpdateApplicationAsync(id, application => application.Withdraw(), ct);

    public async Task<StaffAdmissionApplicationsPageResponse> GetStaffApplicationsAsync(
        StaffAdmissionApplicationsQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search?.Length > 100) search = search[..100];

        var (items, totalCount) = await _repository.GetStaffPageAsync(
            query.Status, query.AcademicProgramId, search, page, pageSize, ct);

        return new StaffAdmissionApplicationsPageResponse(
            items.Select(MapStaff).ToArray(), page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<StaffAdmissionApplicationResponse> GetStaffApplicationAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("A valid application ID is required.", nameof(applicationId));
        var application = await _repository.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");
        return MapStaff(application);
    }

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

    private static ApplicantApplicationResponse MapApplicant(AdmissionApplication x) =>
        new(x.Id, x.ApplicationNumber, x.ApplicantFullName, x.ApplicantEmail, x.PhoneNumber,
            x.AcademicProgramId, x.ApplicationDate, x.Status, x.DecisionRemarks, x.CreatedAtUtc, x.UpdatedAtUtc);

    private static StaffAdmissionApplicationResponse MapStaff(AdmissionApplication x) =>
        new(x.Id, x.ApplicationNumber, x.ApplicantFullName, x.ApplicantEmail, x.PhoneNumber,
            x.AcademicProgramId, x.ApplicationDate, x.Status, x.DecisionRemarks, x.OwnerUserId,
            x.ReviewStartedByUserId, x.ReviewStartedAtUtc, x.DecisionByUserId, x.DecisionAtUtc,
            x.CreatedAtUtc, x.UpdatedAtUtc);
}
