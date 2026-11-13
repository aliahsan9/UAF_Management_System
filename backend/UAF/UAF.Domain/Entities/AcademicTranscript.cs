using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>Immutable, verifiable snapshot of a student's academic record at issuance time.</summary>
public sealed class AcademicTranscript : Entity
{
    private AcademicTranscript() { VerificationCode = string.Empty; SnapshotJson = string.Empty; SnapshotSha256 = string.Empty; }
    public AcademicTranscript(Guid studentId, Guid issuedByUserId, string verificationCode, string snapshotJson, string snapshotSha256, DateTimeOffset issuedAtUtc)
    {
        if (studentId == Guid.Empty || issuedByUserId == Guid.Empty) throw new ArgumentException("Student and issuing officer are required.");
        if (string.IsNullOrWhiteSpace(verificationCode) || verificationCode.Length != 32) throw new ArgumentException("A 128-bit verification code is required.");
        if (string.IsNullOrWhiteSpace(snapshotJson) || snapshotJson.Length > 2_000_000) throw new ArgumentException("Transcript snapshot is required and must not exceed 2 MB.");
        if (string.IsNullOrWhiteSpace(snapshotSha256) || snapshotSha256.Length != 64) throw new ArgumentException("A SHA-256 snapshot digest is required.");
        if (issuedAtUtc == default) throw new ArgumentException("Transcript issue timestamp is required.");
        StudentId = studentId; IssuedByUserId = issuedByUserId; VerificationCode = verificationCode.ToUpperInvariant();
        SnapshotJson = snapshotJson; SnapshotSha256 = snapshotSha256.ToUpperInvariant(); IssuedAtUtc = issuedAtUtc.ToUniversalTime();
    }
    public Guid StudentId { get; private set; }
    public Guid IssuedByUserId { get; private set; }
    public string VerificationCode { get; private set; }
    public string SnapshotJson { get; private set; }
    public string SnapshotSha256 { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
}
