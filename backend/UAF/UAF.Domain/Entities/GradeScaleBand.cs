using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class GradeScaleBand : Entity
{
    private GradeScaleBand() { LetterGrade = string.Empty; }
    public GradeScaleBand(Guid gradeScaleId, string letterGrade, decimal minimumPercentage, decimal gradePoints, bool isPassing)
    {
        if (gradeScaleId == Guid.Empty) throw new ArgumentException("Grade scale is required.", nameof(gradeScaleId));
        if (string.IsNullOrWhiteSpace(letterGrade) || letterGrade.Trim().Length > 10) throw new ArgumentException("Letter grade is required (maximum 10 characters).", nameof(letterGrade));
        if (minimumPercentage < 0 || minimumPercentage > 100) throw new ArgumentOutOfRangeException(nameof(minimumPercentage));
        if (gradePoints < 0 || gradePoints > 10) throw new ArgumentOutOfRangeException(nameof(gradePoints));
        GradeScaleId = gradeScaleId;
        LetterGrade = letterGrade.Trim().ToUpperInvariant();
        MinimumPercentage = minimumPercentage;
        GradePoints = gradePoints;
        IsPassing = isPassing;
    }
    public Guid GradeScaleId { get; private set; }
    public string LetterGrade { get; private set; }
    public decimal MinimumPercentage { get; private set; }
    public decimal GradePoints { get; private set; }
    public bool IsPassing { get; private set; }
}
