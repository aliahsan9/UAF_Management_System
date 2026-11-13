namespace UAF.Infrastructure.Identity;

/// <summary>One-time refresh credential. Only a SHA-256 hash of the secret is persisted.</summary>
public sealed class RefreshToken
{
    private RefreshToken() { TokenHash = string.Empty; }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc, string? createdByIpAddress)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64)
            throw new ArgumentException("A SHA-256 token hash is required.", nameof(tokenHash));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentException("Expiry must follow creation.");
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIpAddress = NormalizeIp(createdByIpAddress);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? CreatedByIpAddress { get; private set; }
    public string? RevokedByIpAddress { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Revoke(DateTimeOffset revokedAtUtc, string? remoteIpAddress, string? replacedByTokenHash)
    {
        if (RevokedAtUtc is not null) return;
        RevokedAtUtc = revokedAtUtc;
        RevokedByIpAddress = NormalizeIp(remoteIpAddress);
        ReplacedByTokenHash = replacedByTokenHash;
    }

    private static string? NormalizeIp(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 64)];
}
