using UAF.Domain.Entities;
using Xunit;
namespace UAF.Tests.Examinations;
public sealed class ExaminationResultTests
{
    [Fact]
    public void Absent_student_must_have_zero_marks()
    {
        Assert.Throws<ArgumentException>(() => new ExaminationResult(Guid.NewGuid(), Guid.NewGuid(), 1m, true, null, Guid.NewGuid()));
    }
    [Fact]
    public void Marks_correction_updates_actor_and_timestamp()
    {
        var result = new ExaminationResult(Guid.NewGuid(), Guid.NewGuid(), 12m, false, null, Guid.NewGuid());
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        result.Update(15m, false, "Rechecked", actor, now);
        Assert.Equal(15m, result.MarksObtained);
        Assert.Equal(actor, result.RecordedByUserId);
        Assert.Equal(now, result.RecordedAtUtc);
    }
}
