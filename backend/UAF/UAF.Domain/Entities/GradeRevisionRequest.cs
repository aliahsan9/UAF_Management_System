using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>A proposed correction to an immutable finalized course grade.</summary>
public sealed class GradeRevisionRequest : AuditableEntity
{
    private GradeRevisionRequest() { Reason = string.Empty; }
    public GradeRevisionRequest(Guid studentCourseGradeId, Guid appealId, decimal proposedMarksObtained, string reason, Guid requestedByUserId, DateTimeOffset requestedAtUtc)
    {
        if (studentCourseGradeId == Guid.Empty || appealId == Guid.Empty || requestedByUserId == Guid.Empty) throw new ArgumentException("Grade, approved appeal and requesting actor are required.");
        if (proposedMarksObtained < 0) throw new ArgumentOutOfRangeException(nameof(proposedMarksObtained));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) throw new ArgumentException("Revision reason is required (maximum 1000 characters).");
        if (requestedAtUtc == default) throw new ArgumentException("Request timestamp is required.");
        StudentCourseGradeId = studentCourseGradeId; AppealId = appealId; ProposedMarksObtained = proposedMarksObtained;
        Reason = reason.Trim(); RequestedByUserId = requestedByUserId; RequestedAtUtc = requestedAtUtc.ToUniversalTime();
        Status = GradeRevisionStatus.PendingApproval;
    }
    public Guid StudentCourseGradeId { get; private set; }
    public Guid AppealId { get; private set; }
    public decimal ProposedMarksObtained { get; private set; }
    public string Reason { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public GradeRevisionStatus Status { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewRemarks { get; private set; }
    public void Decide(bool approve, Guid reviewerId, string remarks, DateTimeOffset now)
    {
        if (Status != GradeRevisionStatus.PendingApproval) throw new InvalidOperationException("Only pending grade revision requests can be decided.");
        if (reviewerId == Guid.Empty || reviewerId == RequestedByUserId) throw new InvalidOperationException("A different authorized reviewer must approve or reject the request.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length > 1000) throw new ArgumentException("Review remarks are required (maximum 1000 characters).");
        if (now == default) throw new ArgumentException("Review timestamp is required.");
        Status = approve ? GradeRevisionStatus.Approved : GradeRevisionStatus.Rejected;
        ReviewedByUserId = reviewerId; ReviewedAtUtc = now.ToUniversalTime(); ReviewRemarks = remarks.Trim(); MarkUpdated(now);
    }
}
