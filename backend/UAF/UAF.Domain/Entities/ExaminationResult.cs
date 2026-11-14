using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>One student's marks for one examination. Absence is explicit and awards zero marks.</summary>
public sealed class ExaminationResult : AuditableEntity
{
    private ExaminationResult() { }
    public ExaminationResult(Guid examinationId, Guid studentId, decimal marksObtained, bool isAbsent, string? remarks, Guid recordedByUserId)
    {
        if (examinationId == Guid.Empty || studentId == Guid.Empty || recordedByUserId == Guid.Empty) throw new ArgumentException("Examination, student and actor are required.");
        if (marksObtained < 0) throw new ArgumentOutOfRangeException(nameof(marksObtained));
        if (isAbsent && marksObtained != 0) throw new ArgumentException("An absent student must receive zero marks.");
        ExaminationId = examinationId;
        StudentId = studentId;
        MarksObtained = marksObtained;
        IsAbsent = isAbsent;
        Remarks = NormalizeRemarks(remarks);
        RecordedByUserId = recordedByUserId;
        RecordedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid ExaminationId { get; private set; }
    public Guid StudentId { get; private set; }
    public decimal MarksObtained { get; private set; }
    public bool IsAbsent { get; private set; }
    public string Remarks { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public void Update(decimal marksObtained, bool isAbsent, string? remarks, Guid actorId, DateTimeOffset now)
    {
        if (actorId == Guid.Empty || now == default) throw new ArgumentException("Authenticated actor and timestamp are required.");
        if (marksObtained < 0) throw new ArgumentOutOfRangeException(nameof(marksObtained));
        if (isAbsent && marksObtained != 0) throw new ArgumentException("An absent student must receive zero marks.");
        MarksObtained = marksObtained;
        IsAbsent = isAbsent;
        Remarks = NormalizeRemarks(remarks);
        RecordedByUserId = actorId;
        RecordedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    private static string NormalizeRemarks(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Trim().Length > 500) throw new ArgumentOutOfRangeException(nameof(value), "Remarks cannot exceed 500 characters.");
        return value.Trim();
    }
}
