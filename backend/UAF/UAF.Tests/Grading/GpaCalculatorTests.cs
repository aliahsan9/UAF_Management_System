using UAF.Application.Features.Grading;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Grading;
public sealed class GpaCalculatorTests
{
    [Fact]
    public void Calculates_credit_weighted_gpa()
    {
        var items = new[] { Item("CS101", 3m, 4m), Item("CS102", 1m, 2m) };
        var result = GpaCalculator.Calculate(items, RepeatCoursePolicy.AllAttempts, true);
        Assert.Equal(3.5m, result.Gpa);
        Assert.Equal(4m, result.CreditHours);
    }
    [Fact]
    public void Latest_attempt_policy_uses_most_recent_course_attempt()
    {
        var first = Item("CS101", 3m, 2m, new DateOnly(2025, 1, 1));
        var latest = Item("CS101", 3m, 4m, new DateOnly(2026, 1, 1));
        var result = GpaCalculator.Calculate(new[] { first, latest }, RepeatCoursePolicy.LatestAttempt, true);
        Assert.Equal(4m, result.Gpa);
        Assert.Equal(3m, result.CreditHours);
    }
    [Fact]
    public void Highest_grade_policy_uses_highest_attempt()
    {
        var items = new[] { Item("CS101", 3m, 3m), Item("CS101", 3m, 4m, new DateOnly(2026, 1, 1)) };
        var result = GpaCalculator.Calculate(items, RepeatCoursePolicy.HighestGrade, true);
        Assert.Equal(4m, result.Gpa);
        Assert.Equal(3m, result.CreditHours);
    }
    [Fact]
    public void Failed_courses_can_be_excluded()
    {
        var items = new[] { Item("CS101", 3m, 4m), Item("CS102", 3m, 0m, isPassed: false) };
        var result = GpaCalculator.Calculate(items, RepeatCoursePolicy.AllAttempts, false);
        Assert.Equal(4m, result.Gpa);
        Assert.Equal(3m, result.CreditHours);
    }
    [Fact]
    public void Returns_null_when_no_eligible_credit_hours()
    {
        var result = GpaCalculator.Calculate(Array.Empty<AcademicRecordCourseItem>(), RepeatCoursePolicy.AllAttempts, true);
        Assert.Null(result.Gpa);
        Assert.Equal(0m, result.CreditHours);
    }
    private static AcademicRecordCourseItem Item(string code, decimal credits, decimal points, DateOnly? date = null, bool isPassed = true) =>
        new(Guid.Parse(code == "CS101" ? "10000000-0000-0000-0000-000000000001" : "20000000-0000-0000-0000-000000000001"),
            Guid.NewGuid(), Guid.NewGuid(), "Semester", date ?? new DateOnly(2025, 1, 1), code, code, credits, 100, points * 20,
            points * 20, "A", points, isPassed, true, RepeatCoursePolicy.AllAttempts, DateTimeOffset.UtcNow);
}
