using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Models;

public sealed record StaffAdmissionApplicationsQuery(
    AdmissionStatus? Status = null,
    Guid? AcademicProgramId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record StaffAdmissionApplicationResponse(
    Guid Id,
    string ApplicationNumber,
    string ApplicantFullName,
    string ApplicantEmail,
    string? PhoneNumber,
    Guid AcademicProgramId,
    DateOnly ApplicationDate,
    AdmissionStatus Status,
    string? DecisionRemarks,
    Guid? OwnerUserId,
    Guid? ReviewStartedByUserId,
    DateTimeOffset? ReviewStartedAtUtc,
    Guid? DecisionByUserId,
    DateTimeOffset? DecisionAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record StaffAdmissionApplicationsPageResponse(
    IReadOnlyList<StaffAdmissionApplicationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
