using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Models;

public sealed record ConfigureRegistrationSeriesRequest(Guid AcademicProgramId, int AdmissionYear, string Prefix, int NextNumber);

public sealed record AdmissionDecisionAuditResponse(
    Guid Id, Guid ApplicationId, Guid ActorUserId, AdmissionStatus FromStatus,
    AdmissionStatus ToStatus, string? Remarks, DateTimeOffset OccurredAtUtc);

public sealed record ConvertedStudentResponse(
    Guid StudentId, Guid AdmissionApplicationId, string RegistrationNumber,
    string FullName, string Email, Guid AcademicProgramId, DateOnly EnrollmentDate,
    Guid? IdentityUserId, Guid RegisteredByUserId, DateTimeOffset RegisteredAtUtc);

public sealed record AdmissionWorkflowResponse(
    Guid ApplicationId, AdmissionStatus Status, Guid? ReviewStartedByUserId,
    DateTimeOffset? ReviewStartedAtUtc, Guid? DecisionByUserId,
    DateTimeOffset? DecisionAtUtc, string? DecisionRemarks);

internal static class AdmissionWorkflowMappings
{
    public static AdmissionDecisionAuditResponse ToResponse(this AdmissionDecisionAudit x) =>
        new(x.Id, x.ApplicationId, x.ActorUserId, x.FromStatus, x.ToStatus, x.Remarks, x.OccurredAtUtc);
}
