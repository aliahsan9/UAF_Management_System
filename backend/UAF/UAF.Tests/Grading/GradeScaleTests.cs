using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Grading;
public sealed class GradeScaleTests
{
    [Fact]
    public void Published_scale_requires_zero_threshold()
    {
        var scale = new GradeScale(Guid.NewGuid(), "Academic grading", false, RepeatCoursePolicy.AllAttempts, Guid.NewGuid());
        scale.AddBand(new GradeScaleBand(scale.Id, "A", 50, 4, true));
        Assert.Throws<InvalidOperationException>(() => scale.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Published_scale_is_immutable()
    {
        var scale = new GradeScale(Guid.NewGuid(), "Academic grading", false, RepeatCoursePolicy.AllAttempts, Guid.NewGuid());
        scale.AddBand(new GradeScaleBand(scale.Id, "F", 0, 0, false));
        scale.AddBand(new GradeScaleBand(scale.Id, "A", 80, 4, true));
        scale.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => scale.AddBand(new GradeScaleBand(scale.Id, "B", 60, 3, true)));
    }
    [Fact]
    public void Rejects_grade_points_above_supported_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GradeScaleBand(Guid.NewGuid(), "A", 80, 11, true));
    }
}
