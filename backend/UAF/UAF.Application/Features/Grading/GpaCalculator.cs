using UAF.Application.Features.Grading.Models;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading;
/// <summary>Pure weighted GPA calculator. Repeated-course policy is explicit and configurable.</summary>
public static class GpaCalculator
{
    public static (decimal? Gpa, decimal CreditHours) Calculate(IEnumerable<AcademicRecordCourseItem> source, RepeatCoursePolicy policy, bool includeFailedCourses)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        var attempts = source.Where(x => includeFailedCourses || x.IsPassed).ToArray();
        var selected = policy switch
        {
            RepeatCoursePolicy.AllAttempts => attempts,
            RepeatCoursePolicy.LatestAttempt => attempts.GroupBy(x => x.CourseId).Select(g => g.OrderByDescending(x => x.AcademicSessionStartDate).ThenByDescending(x => x.FinalizedAtUtc).First()).ToArray(),
            RepeatCoursePolicy.HighestGrade => attempts.GroupBy(x => x.CourseId).Select(g => g.OrderByDescending(x => x.GradePoints).ThenByDescending(x => x.AcademicSessionStartDate).ThenByDescending(x => x.FinalizedAtUtc).First()).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
        var credits = selected.Sum(x => x.CreditHours);
        if (credits <= 0) return (null, 0);
        var qualityPoints = selected.Sum(x => x.GradePoints * x.CreditHours);
        return (decimal.Round(qualityPoints / credits, 2, MidpointRounding.AwayFromZero), credits);
    }
}
