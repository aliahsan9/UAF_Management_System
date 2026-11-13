using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Transcripts.Abstractions;
using UAF.Application.Features.Transcripts.Models;
using UAF.Domain.Entities;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class TranscriptRepository : ITranscriptRepository
{
    private readonly UafDbContext _db;
    public TranscriptRepository(UafDbContext db) => _db = db;
    public Task<Guid?> GetStudentIdForIdentityUserAsync(Guid identityUserId, CancellationToken ct) =>
        _db.Students.Where(x => x.IdentityUserId == identityUserId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    public Task<bool> StudentExistsAsync(Guid studentId, CancellationToken ct) =>
        _db.Students.AnyAsync(x => x.Id == studentId, ct);
    public async Task AddAsync(AcademicTranscript transcript, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await _db.Students.AnyAsync(x => x.Id == transcript.StudentId, ct))
            throw new KeyNotFoundException("Student record not found.");
        _db.AcademicTranscripts.Add(transcript);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<TranscriptListItem>> GetForStudentAsync(Guid studentId, CancellationToken ct) =>
        await _db.AcademicTranscripts.AsNoTracking().Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.IssuedAtUtc)
            .Select(x => new TranscriptListItem(x.Id, x.VerificationCode, x.SnapshotSha256, x.IssuedAtUtc)).ToListAsync(ct);
    public Task<AcademicTranscript?> GetForStudentAsync(Guid transcriptId, Guid studentId, CancellationToken ct) =>
        _db.AcademicTranscripts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == transcriptId && x.StudentId == studentId, ct);
    public Task<AcademicTranscript?> GetByVerificationCodeAsync(string code, CancellationToken ct) =>
        _db.AcademicTranscripts.AsNoTracking().SingleOrDefaultAsync(x => x.VerificationCode == code, ct);
    public Task<string?> GetRegistrationNumberAsync(Guid studentId, CancellationToken ct) =>
        _db.Students.AsNoTracking().Where(x => x.Id == studentId).Select(x => x.RegistrationNumber).SingleOrDefaultAsync(ct);
}
