using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Grading;
public sealed class GradeRevisionTests
{
    [Fact]
    public void Revision_request_requires_a_reason_and_starts_pending()
    {
        var request = new GradeRevisionRequest(Guid.NewGuid(), Guid.NewGuid(), 82m, "Recheck calculation.", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(GradeRevisionStatus.PendingApproval, request.Status);
        Assert.Throws<ArgumentException>(() => new GradeRevisionRequest(Guid.NewGuid(), Guid.NewGuid(), 82m, " ", Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Requester_cannot_approve_their_own_revision()
    {
        var actor = Guid.NewGuid();
        var request = new GradeRevisionRequest(Guid.NewGuid(), Guid.NewGuid(), 82m, "Recheck calculation.", actor, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => request.Decide(true, actor, "Approved.", DateTimeOffset.UtcNow));
        Assert.Equal(GradeRevisionStatus.PendingApproval, request.Status);
    }
    [Fact]
    public void Approved_request_cannot_be_decided_twice()
    {
        var request = new GradeRevisionRequest(Guid.NewGuid(), Guid.NewGuid(), 82m, "Recheck calculation.", Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.Decide(true, Guid.NewGuid(), "Evidence verified.", DateTimeOffset.UtcNow);
        Assert.Equal(GradeRevisionStatus.Approved, request.Status);
        Assert.Throws<InvalidOperationException>(() => request.Decide(false, Guid.NewGuid(), "Reject.", DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Revision_snapshot_preserves_before_and_after_values()
    {
        var revision = new GradeRevision(Guid.NewGuid(), Guid.NewGuid(), 1, 72m, 82m, 72m, 82m,
            "B", "A", 3m, 4m, true, true, "Approved re-evaluation.", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(72m, revision.PreviousMarks);
        Assert.Equal(82m, revision.NewMarks);
        Assert.Equal("B", revision.PreviousLetterGrade);
        Assert.Equal("A", revision.NewLetterGrade);
    }
    [Fact]
    public void Revision_audit_only_accepts_pending_to_approved_or_rejected()
    {
        Assert.Throws<ArgumentException>(() => new GradeRevisionAudit(Guid.NewGuid(), GradeRevisionStatus.Approved,
            GradeRevisionStatus.Rejected, Guid.NewGuid(), "Invalid transition.", DateTimeOffset.UtcNow));
    }
}
