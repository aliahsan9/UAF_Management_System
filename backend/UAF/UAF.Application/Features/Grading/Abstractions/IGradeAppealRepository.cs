using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Grading.Abstractions;
public interface IGradeAppealRepository
{
    Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct);
    Task<bool> GradeBelongsToStudentAsync(Guid gradeId, Guid studentId, CancellationToken ct);
    Task<bool> HasPendingAppealAsync(Guid gradeId, CancellationToken ct);
    Task AddAppealAsync(GradeAppeal appeal, CancellationToken ct);
    Task<IReadOnlyList<GradeAppealResponse>> GetStudentAppealsAsync(Guid studentId, CancellationToken ct);
    Task<GradeAppeal?> GetAppealAsync(Guid appealId, CancellationToken ct);
    Task<bool> CanReviewAppealAsync(Guid appealId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct);
    Task<IReadOnlyList<GradeAppealResponse>> GetPendingAppealsAsync(Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct);
    Task DecideAppealAsync(GradeAppeal appeal, GradeAppealAudit audit, CancellationToken ct);
    Task<IReadOnlyList<GradeAppealAuditResponse>> GetAppealHistoryAsync(Guid appealId, CancellationToken ct);
}
