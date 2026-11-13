using UAF.Domain.Entities;
namespace UAF.Application.Features.Transcripts.Abstractions;
public interface ITranscriptRepository
{
    Task<bool> StudentExistsAsync(Guid studentId, CancellationToken ct);
    Task AddAsync(AcademicTranscript transcript, CancellationToken ct);
    Task<IReadOnlyList<AcademicTranscript>> GetForStudentAsync(Guid studentId, CancellationToken ct);
    Task<AcademicTranscript?> GetForStudentAsync(Guid transcriptId, Guid studentId, CancellationToken ct);
    Task<AcademicTranscript?> GetByVerificationCodeAsync(string code, CancellationToken ct);
    Task<string?> GetRegistrationNumberAsync(Guid studentId, CancellationToken ct);
}
