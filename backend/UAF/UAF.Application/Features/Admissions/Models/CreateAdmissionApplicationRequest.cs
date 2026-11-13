namespace UAF.Application.Features.Admissions.Models;

/// <summary>
/// Input required to create an admission application draft.
/// HTTP-specific validation and mapping will be handled at the API boundary.
/// </summary>
public sealed record CreateAdmissionApplicationRequest(
    string ApplicationNumber,
    string ApplicantFullName,
    string ApplicantEmail,
    Guid AcademicProgramId,
    DateOnly ApplicationDate,
    string? PhoneNumber = null);
