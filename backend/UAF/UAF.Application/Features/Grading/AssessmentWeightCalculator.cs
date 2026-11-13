using UAF.Application.Features.Grading.Models;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Grading;
/// <summary>Calculates a course percentage from published, weighted assessment results.</summary>
public static class AssessmentWeightCalculator
{
    public static decimal Calculate(IEnumerable<AssessmentGradeItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var items = source.ToArray();
        if (items.Length == 0) throw new InvalidOperationException("No assessment components are configured for this course offering.");
        if (items.Any(x => x.Status != ExaminationStatus.Published))
            throw new InvalidOperationException("Every configured assessment must be published before a course grade can be finalized.");
        if (items.Any(x => !x.MarksObtained.HasValue || !x.IsAbsent.HasValue))
            throw new InvalidOperationException("A marks record is required for every published assessment.");
        if (items.Any(x => x.MaximumMarks <= 0 || x.WeightPercentage <= 0 || x.WeightPercentage > 100
            || x.MarksObtained < 0 || x.MarksObtained > x.MaximumMarks || (x.IsAbsent == true && x.MarksObtained != 0)))
            throw new ArgumentOutOfRangeException(nameof(source), "Assessment marks, maximum marks and weights must be valid.");
        var totalWeight = items.Sum(x => x.WeightPercentage);
        if (totalWeight != 100m)
            throw new InvalidOperationException($"Assessment weights must total exactly 100 percent; configured total is {totalWeight:0.##}%.");
        var percentage = items.Sum(x => (x.MarksObtained!.Value / x.MaximumMarks) * x.WeightPercentage);
        return decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
    }
}
