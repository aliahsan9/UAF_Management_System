namespace UAF.Application.Features.Authentication.Models;

public sealed record AuthenticationResult(
    bool Succeeded,
    string? Error,
    string? AccessToken,
    DateTimeOffset? ExpiresAtUtc,
    string? UserId,
    string? Email,
    IReadOnlyCollection<string> Roles,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAtUtc = null)
{
    public static AuthenticationResult Failure(string error) =>
        new(false, error, null, null, null, null, Array.Empty<string>());
}
