using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Admissions.Abstractions;

public interface IAdmissionWorkflowRepository
{
    Task StartReviewAsync(Guid applicationId, Guid actorUserId, DateTimeOffset occurredAtUtc, CancellationToken ct = default);
    Task RecordDecisionAsync(Guid applicationId, Guid actorUserId, AdmissionStatus decision, string? remarks, DateTimeOffset occurredAtUtc, CancellationToken ct = default);
    Task ConfigureRegistrationSeriesAsync(Guid academicProgramId, int admissionYear, string prefix, int nextNumber, Guid actorUserId, CancellationToken ct = default);
    Task<Student> ConvertAcceptedApplicationAsync(Guid applicationId, Guid actorUserId, DateTimeOffset occurredAtUtc, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionDecisionAudit>> GetDecisionHistoryAsync(Guid applicationId, CancellationToken ct = default);
}
