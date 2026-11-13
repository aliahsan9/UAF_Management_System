using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Examinations;
public sealed class ExaminationDomainTests
{
    [Fact]
    public void New_examination_starts_as_draft_and_normalizes_title()
    {
        var exam = new Examination(Guid.NewGuid(), "  Mid Term  ", ExaminationType.Midterm, new DateOnly(2026, 11, 20), 50m, 20m);
        Assert.Equal("Mid Term", exam.Title);
        Assert.Equal(ExaminationStatus.Draft, exam.Status);
    }
    [Fact]
    public void Workflow_requires_schedule_then_marks_submission_approval_and_publication()
    {
        var exam = Create();
        exam.Schedule();
        exam.OpenMarksEntry();
        var submitter = Guid.NewGuid();
        var approver = Guid.NewGuid();
        exam.Submit(submitter, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => exam.Approve(submitter, DateTimeOffset.UtcNow));
        exam.Approve(approver, DateTimeOffset.UtcNow);
        exam.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(ExaminationStatus.Published, exam.Status);
    }
    [Fact]
    public void Cannot_publish_before_approval()
    {
        var exam = Create();
        Assert.Throws<InvalidOperationException>(() => exam.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
    [Theory]
    [InlineData(0, 0)]
    [InlineData(10001, 50)]
    [InlineData(50, 51)]
    public void Rejects_invalid_mark_boundaries(decimal maximum, decimal passing)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Examination(Guid.NewGuid(), "Final", ExaminationType.Final, new DateOnly(2026, 12, 1), maximum, passing));
    }
    [Fact]
    public void Cannot_cancel_submitted_examination()
    {
        var exam = Create();
        exam.Schedule(); exam.OpenMarksEntry(); exam.Submit(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => exam.Cancel());
    }
    private static Examination Create() => new(Guid.NewGuid(), "Midterm", ExaminationType.Midterm, new DateOnly(2026, 11, 20), 50m, 20m);
}
