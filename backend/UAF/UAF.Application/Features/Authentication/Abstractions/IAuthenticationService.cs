using UAF.Application.Features.Authentication.Models;

namespace UAF.Application.Features.Authentication.Abstractions;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
