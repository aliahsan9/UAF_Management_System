using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Grading;
public sealed class GradeAppealTests
{
    [Fact]
    public void Appeal_starts_pending_and_can_be_approved_once()
    {
        var appeal = new GradeAppeal(Guid.NewGuid(), Guid.NewGuid(), "Please recheck the published marks.", DateTimeOffset.UtcNow);
        Assert.Equal(GradeAppealStatus.Pending, appeal.Status);
        appeal.Decide(true, Guid.NewGuid(), "Evidence reviewed; approve re-evaluation.", DateTimeOffset.UtcNow);
        Assert.Equal(GradeAppealStatus.Approved, appeal.Status);
        Assert.Throws<InvalidOperationException>(() => appeal.Decide(false, Guid.NewGuid(), "Second decision.", DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Appeal_decision_requires_remarks()
    {
        var appeal = new GradeAppeal(Guid.NewGuid(), Guid.NewGuid(), "Please recheck the published marks.", DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => appeal.Decide(true, Guid.NewGuid(), " ", DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Audit_captures_status_transition_and_actor()
    {
        var actor = Guid.NewGuid();
        var audit = new GradeAppealAudit(Guid.NewGuid(), GradeAppealStatus.Pending, GradeAppealStatus.Rejected,
            actor, "No supporting evidence was provided.", DateTimeOffset.UtcNow);
        Assert.Equal(GradeAppealStatus.Pending, audit.PreviousStatus);
        Assert.Equal(GradeAppealStatus.Rejected, audit.NewStatus);
        Assert.Equal(actor, audit.ActorId);
    }
}
