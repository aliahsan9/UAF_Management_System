using UAF.Domain.Entities;
using Xunit;
namespace UAF.Tests.Attendance;
public sealed class AttendancePolicyTests
{
    [Fact]
    public void Policy_accepts_valid_configurable_rules()
    {
        var policy = new AttendancePolicy(Guid.NewGuid(), 75m, 50m, true, Guid.NewGuid());
        Assert.Equal(75m, policy.RequiredPercentage);
        Assert.Equal(50m, policy.LateCreditPercentage);
        Assert.True(policy.ExcludeExcusedFromDenominator);
    }
    [Theory]
    [InlineData(-1, 50)]
    [InlineData(101, 50)]
    [InlineData(75, -1)]
    [InlineData(75, 101)]
    public void Policy_rejects_percentages_outside_zero_to_hundred(decimal required, decimal lateCredit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AttendancePolicy(Guid.NewGuid(), required, lateCredit, true, Guid.NewGuid()));
    }
}
