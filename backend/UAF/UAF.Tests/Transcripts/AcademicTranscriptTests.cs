using UAF.Domain.Entities;
using Xunit;
namespace UAF.Tests.Transcripts;
public sealed class AcademicTranscriptTests
{
    [Fact]
    public void Transcript_requires_a_32_character_verification_code_and_sha256_digest()
    {
        Assert.Throws<ArgumentException>(() => new AcademicTranscript(Guid.NewGuid(), Guid.NewGuid(), "short", "{}", new string('A', 64), DateTimeOffset.UtcNow));
        var transcript = new AcademicTranscript(Guid.NewGuid(), Guid.NewGuid(), new string('a', 32), "{\"snapshot\":true}", new string('b', 64), DateTimeOffset.UtcNow);
        Assert.Equal(new string('A', 32), transcript.VerificationCode);
        Assert.Equal(new string('B', 64), transcript.SnapshotSha256);
    }
}
