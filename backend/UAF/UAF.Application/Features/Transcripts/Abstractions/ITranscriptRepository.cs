using UAF.Domain.Entities;
using UAF.Application.Features.Transcripts.Models;
namespace UAF.Application.Features.Transcripts.Abstractions;
public interface ITranscriptRepository
{
    Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct);
    Task<bool> StudentExistsAsync(Guid studentId, CancellationToken ct);
    Task AddAsync(AcademicTranscript transcript, CancellationToken ct);
    Task<IReadOnlyList<TranscriptListItem>> GetForStudentAsync(Guid studentId, CancellationToken ct);
    Task<AcademicTranscript?> GetForStudentAsync(Guid transcriptId, Guid studentId, CancellationToken ct);
    Task<AcademicTranscript?> GetByVerificationCodeAsync(string code, CancellationToken ct);
    Task<string?> GetRegistrationNumberAsync(Guid studentId, CancellationToken ct);
}
