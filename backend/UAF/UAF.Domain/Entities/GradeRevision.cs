using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>Immutable before/after snapshot created only when a grade revision is approved.</summary>
public sealed class GradeRevision : Entity
{
    private GradeRevision() { Reason = string.Empty; PreviousLetterGrade = string.Empty; NewLetterGrade = string.Empty; }
    public GradeRevision(Guid requestId, Guid studentCourseGradeId, int revisionNumber,
        decimal previousMarks, decimal newMarks, decimal previousPercentage, decimal newPercentage,
        string previousLetterGrade, string newLetterGrade, decimal previousGradePoints, decimal newGradePoints,
        bool previousIsPassed, bool newIsPassed, string reason, Guid requestedByUserId, Guid approvedByUserId, DateTimeOffset approvedAtUtc)
    {
        if (requestId == Guid.Empty || studentCourseGradeId == Guid.Empty || requestedByUserId == Guid.Empty || approvedByUserId == Guid.Empty)
            throw new ArgumentException("Revision request, course grade and both actors are required.");
        if (revisionNumber < 1) throw new ArgumentOutOfRangeException(nameof(revisionNumber));
        if (newMarks < 0 || previousMarks < 0 || previousPercentage is < 0 or > 100 || newPercentage is < 0 or > 100 ||
            previousGradePoints is < 0 or > 10 || newGradePoints is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(newMarks));
        if (string.IsNullOrWhiteSpace(previousLetterGrade) || string.IsNullOrWhiteSpace(newLetterGrade)) throw new ArgumentException("Grade letters are required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) throw new ArgumentException("Revision reason is required.");
        if (approvedAtUtc == default) throw new ArgumentException("Approval timestamp is required.");
        RequestId = requestId; StudentCourseGradeId = studentCourseGradeId; RevisionNumber = revisionNumber;
        PreviousMarks = previousMarks; NewMarks = newMarks; PreviousPercentage = previousPercentage; NewPercentage = newPercentage;
        PreviousLetterGrade = previousLetterGrade; NewLetterGrade = newLetterGrade; PreviousGradePoints = previousGradePoints; NewGradePoints = newGradePoints;
        PreviousIsPassed = previousIsPassed; NewIsPassed = newIsPassed; Reason = reason.Trim();
        RequestedByUserId = requestedByUserId; ApprovedByUserId = approvedByUserId; ApprovedAtUtc = approvedAtUtc.ToUniversalTime();
    }
    public Guid RequestId { get; private set; }
    public Guid StudentCourseGradeId { get; private set; }
    public int RevisionNumber { get; private set; }
    public decimal PreviousMarks { get; private set; }
    public decimal NewMarks { get; private set; }
    public decimal PreviousPercentage { get; private set; }
    public decimal NewPercentage { get; private set; }
    public string PreviousLetterGrade { get; private set; }
    public string NewLetterGrade { get; private set; }
    public decimal PreviousGradePoints { get; private set; }
    public decimal NewGradePoints { get; private set; }
    public bool PreviousIsPassed { get; private set; }
    public bool NewIsPassed { get; private set; }
    public string Reason { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public Guid ApprovedByUserId { get; private set; }
    public DateTimeOffset ApprovedAtUtc { get; private set; }
}
