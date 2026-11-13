using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UAF.Application.Features.Grading;
using UAF.Application.Features.Transcripts.Abstractions;
using UAF.Application.Features.Transcripts.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Transcripts;
public sealed class TranscriptService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITranscriptRepository _repository;
    private readonly GradingService _grading;
    private readonly ITranscriptPdfGenerator _pdf;
    public TranscriptService(ITranscriptRepository repository, GradingService grading, ITranscriptPdfGenerator pdf)
    { _repository = repository; _grading = grading; _pdf = pdf; }

    public async Task<TranscriptIssuedResponse> IssueAsync(Guid studentId, Guid issuerId, CancellationToken ct)
    {
        if (studentId == Guid.Empty || issuerId == Guid.Empty) throw new ArgumentException("Student and issuing officer are required.");
        var record = await _grading.GetAcademicRecordForStudentAsync(studentId, ct);
        if (record.CourseAttempts.Count == 0) throw new InvalidOperationException("A transcript can only be issued after at least one course grade has been finalized.");
        var snapshot = JsonSerializer.Serialize(record, JsonOptions);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var transcript = new AcademicTranscript(studentId, issuerId, code, snapshot, digest, DateTimeOffset.UtcNow);
        await _repository.AddAsync(transcript, ct);
        return new TranscriptIssuedResponse(transcript.Id, transcript.VerificationCode, transcript.SnapshotSha256, record.RegistrationNumber, transcript.IssuedAtUtc);
    }

    public async Task<IReadOnlyList<TranscriptListItem>> GetMineAsync(Guid actorId, CancellationToken ct)
    {
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        var items = await _repository.GetForStudentAsync(studentId, ct);
        return items;
    }

    public async Task<byte[]> GetMyPdfAsync(Guid transcriptId, Guid actorId, CancellationToken ct)
    {
        var studentId = await _repository.GetStudentIdForIdentityUserAsync(actorId, ct)
            ?? throw new KeyNotFoundException("No student record is linked to this account.");
        var transcript = await _repository.GetForStudentAsync(transcriptId, studentId, ct)
            ?? throw new KeyNotFoundException("Transcript not found.");
        var record = DeserializeAndVerify(transcript);
        return _pdf.Generate(record, transcript.VerificationCode, transcript.SnapshotSha256);
    }

    public async Task<TranscriptVerificationResponse> VerifyAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 32)
            return new TranscriptVerificationResponse(false, null, null, null);
        var transcript = await _repository.GetByVerificationCodeAsync(code.Trim().ToUpperInvariant(), ct);
        if (transcript is null) return new TranscriptVerificationResponse(false, null, null, null);
        try
        {
            var record = DeserializeAndVerify(transcript);
            return new TranscriptVerificationResponse(true, record.RegistrationNumber, transcript.IssuedAtUtc, transcript.SnapshotSha256);
        }
        catch (InvalidOperationException)
        {
            return new TranscriptVerificationResponse(false, null, null, null);
        }
    }

    private static UAF.Application.Features.Grading.Models.StudentAcademicRecordResponse DeserializeAndVerify(AcademicTranscript transcript)
    {
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(transcript.SnapshotJson));
        var expected = Convert.FromHexString(transcript.SnapshotSha256);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            throw new InvalidOperationException("Transcript snapshot integrity verification failed.");
        return JsonSerializer.Deserialize<UAF.Application.Features.Grading.Models.StudentAcademicRecordResponse>(transcript.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Transcript snapshot cannot be read.");
    }
}
