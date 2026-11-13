namespace UAF.Application.Features.Attendance;
/// <summary>Pure attendance percentage calculation shared by reports and tests.</summary>
public static class AttendancePercentageCalculator
{
    public static decimal? Calculate(int finalizedSessions, int present, int late, int excused,
        decimal lateCreditPercentage, bool excludeExcusedFromDenominator)
    {
        if (finalizedSessions < 0 || present < 0 || late < 0 || excused < 0)
            throw new ArgumentOutOfRangeException(nameof(finalizedSessions), "Session counts cannot be negative.");
        if (present + late + excused > finalizedSessions)
            throw new ArgumentException("Attendance status counts cannot exceed finalized sessions.");
        if (lateCreditPercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(lateCreditPercentage));
        var denominator = finalizedSessions - (excludeExcusedFromDenominator ? excused : 0);
        if (denominator == 0) return null;
        var earned = present + (late * lateCreditPercentage / 100m);
        return Math.Round(earned / denominator * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
