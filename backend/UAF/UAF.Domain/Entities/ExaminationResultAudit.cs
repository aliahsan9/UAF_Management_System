using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>Append-only record of every marks entry or correction.</summary>
public sealed class ExaminationResultAudit : Entity
{
    private ExaminationResultAudit() { Reason = string.Empty; }
    public ExaminationResultAudit(Guid examinationId, Guid studentId, decimal? previousMarks, decimal newMarks, bool previousAbsent, bool newAbsent, string reason, Guid actorId, DateTimeOffset changedAtUtc)
    {
        if (examinationId == Guid.Empty || studentId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Examination, student and actor are required.");
        if (newMarks < 0 || (previousMarks.HasValue && previousMarks.Value < 0)) throw new ArgumentOutOfRangeException(nameof(newMarks));
        if ((newAbsent && newMarks != 0) || (previousAbsent && previousMarks.HasValue && previousMarks.Value != 0)) throw new ArgumentException("Absent states must have zero marks.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new ArgumentException("An audit reason is required (maximum 500 characters).", nameof(reason));
        if (changedAtUtc == default) throw new ArgumentException("Audit timestamp is required.", nameof(changedAtUtc));
        ExaminationId = examinationId; StudentId = studentId; PreviousMarks = previousMarks; NewMarks = newMarks;
        PreviousAbsent = previousAbsent; NewAbsent = newAbsent; Reason = reason.Trim(); ActorId = actorId; ChangedAtUtc = changedAtUtc.ToUniversalTime();
    }
    public Guid ExaminationId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal? PreviousMarks { get; private set; }
    public decimal NewMarks { get; private set; }
    public bool PreviousAbsent { get; private set; }
    public bool NewAbsent { get; private set; }
    public string Reason { get; private set; }
    public Guid ActorId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
}
