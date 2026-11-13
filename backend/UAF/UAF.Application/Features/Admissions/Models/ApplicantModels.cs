using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Models;

public sealed record CreateApplicantDraftRequest(
    string ApplicantFullName,
    string ApplicantEmail,
    Guid AcademicProgramId,
    string? PhoneNumber);

public sealed record UpdateApplicantDraftRequest(
    string ApplicantFullName,
    string ApplicantEmail,
    string? PhoneNumber);

public sealed record ApplicantApplicationResponse(
    Guid Id,
    string ApplicationNumber,
    string ApplicantFullName,
    string ApplicantEmail,
    string? PhoneNumber,
    Guid AcademicProgramId,
    DateOnly ApplicationDate,
    AdmissionStatus Status,
    string? DecisionRemarks,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
