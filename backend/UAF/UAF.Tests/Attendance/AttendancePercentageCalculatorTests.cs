using UAF.Application.Features.Attendance;
using Xunit;
namespace UAF.Tests.Attendance;
public sealed class AttendancePercentageCalculatorTests
{
    [Fact]
    public void Calculates_percentage_using_configured_late_credit()
    {
        var result = AttendancePercentageCalculator.Calculate(10, 6, 2, 1, 50m, true);
        Assert.Equal(77.78m, result);
    }
    [Fact]
    public void Excused_sessions_can_remain_in_denominator_when_policy_says_so()
    {
        var result = AttendancePercentageCalculator.Calculate(10, 6, 2, 1, 50m, false);
        Assert.Equal(70m, result);
    }
    [Fact]
    public void Returns_null_when_no_sessions_are_counted()
    {
        Assert.Null(AttendancePercentageCalculator.Calculate(2, 0, 0, 2, 50m, true));
    }
    [Fact]
    public void Rejects_counts_that_exceed_finalized_sessions()
    {
        Assert.Throws<ArgumentException>(() => AttendancePercentageCalculator.Calculate(2, 1, 1, 1, 50m, true));
    }
    [Fact]
    public void Rejects_negative_counts_and_invalid_late_credit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AttendancePercentageCalculator.Calculate(-1, 0, 0, 0, 50m, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => AttendancePercentageCalculator.Calculate(2, 1, 0, 0, 101m, true));
    }
}
