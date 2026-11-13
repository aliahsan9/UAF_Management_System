namespace UAF.Application.Features.Transcripts.Models;
public sealed record TranscriptIssuedResponse(Guid Id, string VerificationCode, string SnapshotSha256, string RegistrationNumber, DateTimeOffset IssuedAtUtc);
public sealed record TranscriptVerificationResponse(bool IsAuthentic, string RegistrationNumber, DateTimeOffset IssuedAtUtc, string SnapshotSha256);
public sealed record TranscriptListItem(Guid Id, string VerificationCode, string SnapshotSha256, DateTimeOffset IssuedAtUtc);
