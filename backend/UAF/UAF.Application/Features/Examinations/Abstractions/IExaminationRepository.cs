using UAF.Application.Features.Examinations.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Examinations.Abstractions;
public interface IExaminationRepository
{
    Task<bool> CanManageOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct, bool allowClosed = false);
    Task<bool> CanAdministerOfferingAsync(Guid offeringId, Guid actorId, bool privileged, bool departmentAdmin, CancellationToken ct);
    Task AddAsync(Examination examination, CancellationToken ct);
    Task<Examination?> GetAsync(Guid examinationId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid offeringId, string title, CancellationToken ct);
    Task<bool> IsScheduledDateWithinSessionAsync(Guid offeringId, DateOnly scheduledDate, CancellationToken ct);
    Task SaveMarksAsync(Guid examinationId, Guid actorId, IReadOnlyCollection<ExaminationMarkItem> marks, CancellationToken ct);
    Task SubmitAsync(Guid examinationId, Guid actorId, CancellationToken ct);
    Task ApproveAsync(Guid examinationId, Guid actorId, CancellationToken ct);
    Task PublishAsync(Guid examinationId, Guid actorId, CancellationToken ct);
    Task<IReadOnlyList<ExaminationResultView>> GetResultsAsync(Guid examinationId, CancellationToken ct);
    Task<IReadOnlyList<ExaminationListItem>> GetOfferingExaminationsAsync(Guid offeringId, CancellationToken ct);
}
