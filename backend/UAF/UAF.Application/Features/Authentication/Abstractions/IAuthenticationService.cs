using UAF.Application.Features.Authentication.Models;

namespace UAF.Application.Features.Authentication.Abstractions;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, string? remoteIpAddress = null, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> LoginAsync(LoginRequest request, string? remoteIpAddress = null, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RefreshAsync(string refreshToken, string? remoteIpAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string refreshToken, string? remoteIpAddress, CancellationToken cancellationToken = default);
}
