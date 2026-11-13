using UAF.Application.Features.Grading.Models;
namespace UAF.Application.Features.Transcripts.Abstractions;
public interface ITranscriptPdfGenerator
{
    byte[] Generate(StudentAcademicRecordResponse record, string verificationCode, string snapshotSha256);
}
