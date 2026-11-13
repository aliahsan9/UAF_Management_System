using UAF.Infrastructure.Identity;
using Xunit;

namespace UAF.Tests.Identity;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Revoke_StoresRevocationAndReplacementMetadata()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var token = new RefreshToken(Guid.NewGuid(), new string('A', 64),
            createdAt, createdAt.AddDays(14), "192.0.2.10");

        token.Revoke(createdAt.AddMinutes(1), "192.0.2.20", new string('B', 64));

        Assert.Equal(createdAt.AddMinutes(1), token.RevokedAtUtc);
        Assert.Equal("192.0.2.20", token.RevokedByIpAddress);
        Assert.Equal(new string('B', 64), token.ReplacedByTokenHash);
        Assert.Equal("192.0.2.10", token.CreatedByIpAddress);
    }

    [Fact]
    public void Revoke_IsIdempotentAndDoesNotOverwriteOriginalMetadata()
    {
        var token = new RefreshToken(Guid.NewGuid(), new string('A', 64),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14), "192.0.2.10");
        var firstRevocation = DateTimeOffset.UtcNow.AddMinutes(1);
        token.Revoke(firstRevocation, "192.0.2.20", null);

        token.Revoke(firstRevocation.AddMinutes(1), "192.0.2.30", new string('B', 64));

        Assert.Equal(firstRevocation, token.RevokedAtUtc);
        Assert.Equal("192.0.2.20", token.RevokedByIpAddress);
        Assert.Null(token.ReplacedByTokenHash);
    }
}
