using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Entities;

namespace UAF.Application.Features.Admissions;

/// <summary>
/// Coordinates admission application use cases without depending on a database or web framework.
/// </summary>
public sealed class AdmissionApplicationService
{
    private readonly IAdmissionApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AdmissionApplicationService(
        IAdmissionApplicationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> CreateDraftAsync(
        CreateAdmissionApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedApplicationNumber = request.ApplicationNumber?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedApplicationNumber))
            throw new ArgumentException("Application number is required.", nameof(request));

        var alreadyExists = await _repository.ExistsByApplicationNumberAsync(
            normalizedApplicationNumber,
            cancellationToken);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"An admission application with number '{normalizedApplicationNumber}' already exists.");

        var application = new AdmissionApplication(
            normalizedApplicationNumber,
            request.ApplicantFullName,
            request.ApplicantEmail,
            request.AcademicProgramId,
            request.ApplicationDate,
            request.PhoneNumber);

        await _repository.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return application.Id;
    }

    public async Task SubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("A valid application ID is required.", nameof(applicationId));

        var application = await _repository.GetByIdAsync(applicationId, cancellationToken);

        if (application is null)
            throw new KeyNotFoundException($"Admission application '{applicationId}' was not found.");

        application.Submit();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
