using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>A student's formal request to review a published course grade.</summary>
public sealed class GradeAppeal : AuditableEntity
{
    private GradeAppeal() { Reason = string.Empty; }
    public GradeAppeal(Guid studentCourseGradeId, Guid studentId, string reason, DateTimeOffset submittedAtUtc)
    {
        if (studentCourseGradeId == Guid.Empty || studentId == Guid.Empty) throw new ArgumentException("Grade and student are required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) throw new ArgumentException("Appeal reason is required (maximum 1000 characters).");
        if (submittedAtUtc == default) throw new ArgumentException("Submission timestamp is required.");
        StudentCourseGradeId = studentCourseGradeId; StudentId = studentId; Reason = reason.Trim();
        Status = GradeAppealStatus.Pending; SubmittedAtUtc = submittedAtUtc.ToUniversalTime();
    }
    public Guid StudentCourseGradeId { get; private set; }
    public Guid StudentId { get; private set; }
    public string Reason { get; private set; }
    public GradeAppealStatus Status { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewRemarks { get; private set; }
    public void Decide(bool approve, Guid reviewerId, string remarks, DateTimeOffset now)
    {
        if (Status != GradeAppealStatus.Pending) throw new InvalidOperationException("Only pending grade appeals can be decided.");
        if (reviewerId == Guid.Empty || now == default) throw new ArgumentException("Reviewer and timestamp are required.");
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length > 1000) throw new ArgumentException("Decision remarks are required (maximum 1000 characters).");
        Status = approve ? GradeAppealStatus.Approved : GradeAppealStatus.Rejected;
        ReviewedByUserId = reviewerId; ReviewedAtUtc = now.ToUniversalTime(); ReviewRemarks = remarks.Trim();
        MarkUpdated(now);
    }
}
