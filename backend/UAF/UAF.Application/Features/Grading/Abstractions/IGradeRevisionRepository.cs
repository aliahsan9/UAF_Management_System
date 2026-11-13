using UAF.Application.Features.Grading.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Grading.Abstractions;
public sealed record GradeRevisionContext(StudentCourseGrade Grade, GradeScale Scale, GradeRevision? LatestRevision);
public interface IGradeRevisionRepository
{
    Task<bool> CanRequestRevisionAsync(Guid gradeId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct);
    Task<GradeRevisionContext?> GetRevisionContextAsync(Guid gradeId, CancellationToken ct);
    Task<bool> ApprovedAppealExistsAsync(Guid appealId, Guid gradeId, CancellationToken ct);
    Task<bool> HasPendingRequestAsync(Guid gradeId, CancellationToken ct);
    Task AddRequestAsync(GradeRevisionRequest request, CancellationToken ct);
    Task<IReadOnlyList<GradeRevisionRequestResponse>> GetPendingRequestsAsync(Guid actorId, bool privileged, CancellationToken ct);
    Task<GradeRevisionRequest?> GetRequestAsync(Guid requestId, CancellationToken ct);
    Task DecideRequestAsync(Guid requestId, Guid actorId, bool approve, string remarks, GradeRevision? revision, GradeRevisionAudit audit, int expectedRevisionNumber, CancellationToken ct);
    Task<bool> StudentOwnsGradeAsync(Guid gradeId, Guid studentId, CancellationToken ct);
    Task<IReadOnlyList<GradeRevisionResponse>> GetRevisionHistoryAsync(Guid gradeId, CancellationToken ct);
}
