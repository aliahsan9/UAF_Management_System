using UAF.Application.Features.Grading;
using UAF.Application.Features.Grading.Models;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Grading;
public sealed class AssessmentWeightCalculatorTests
{
    [Fact]
    public void Calculates_weighted_course_percentage()
    {
        var result = AssessmentWeightCalculator.Calculate(new[]
        {
            Item("Midterm", 40m, 80m, 100m),
            Item("Final", 60m, 90m, 100m)
        });
        Assert.Equal(86m, result);
    }
    [Fact]
    public void Rejects_weights_that_do_not_total_one_hundred()
    {
        Assert.Throws<InvalidOperationException>(() => AssessmentWeightCalculator.Calculate(new[]
        {
            Item("Midterm", 40m, 80m, 100m),
            Item("Final", 50m, 90m, 100m)
        }));
    }
    [Fact]
    public void Rejects_unpublished_assessment()
    {
        var item = Item("Midterm", 100m, 80m, 100m) with { Status = ExaminationStatus.Approved };
        Assert.Throws<InvalidOperationException>(() => AssessmentWeightCalculator.Calculate(new[] { item }));
    }
    [Fact]
    public void Rejects_missing_marks()
    {
        var item = Item("Midterm", 100m, null, 100m);
        Assert.Throws<InvalidOperationException>(() => AssessmentWeightCalculator.Calculate(new[] { item }));
    }
    [Fact]
    public void Rejects_marks_above_maximum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AssessmentWeightCalculator.Calculate(new[]
        {
            Item("Midterm", 100m, 101m, 100m)
        }));
    }
    private static AssessmentGradeItem Item(string title, decimal weight, decimal? marks, decimal maximum) =>
        new(Guid.NewGuid(), title, weight, maximum, marks, false, ExaminationStatus.Published);
}
