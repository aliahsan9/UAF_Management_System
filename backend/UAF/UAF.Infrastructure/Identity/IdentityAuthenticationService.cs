using Microsoft.AspNetCore.Identity;
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Application.Features.Authentication.Models;

namespace UAF.Infrastructure.Identity;

public sealed class IdentityAuthenticationService : IAuthenticationService
{
    private const string StudentRole = "Student";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public IdentityAuthenticationService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
            return AuthenticationResult.Failure("An account with this email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            EmailConfirmed = false,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
            return AuthenticationResult.Failure(string.Join(" ", createResult.Errors.Select(error => error.Description)));

        // Public registration can never choose an elevated role.
        var roleResult = await _userManager.AddToRoleAsync(user, StudentRole);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return AuthenticationResult.Failure("The account could not be assigned its default role.");
        }

        return await CreateAuthenticationResultAsync(user);
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        // Use a generic response to avoid disclosing whether an email exists.
        if (user is null || !user.IsActive)
            return AuthenticationResult.Failure("Invalid email or password.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!validPassword)
            return AuthenticationResult.Failure("Invalid email or password.");

        return await CreateAuthenticationResultAsync(user);
    }

    private async Task<AuthenticationResult> CreateAuthenticationResultAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _tokenGenerator.Generate(user, roles);

        return new AuthenticationResult(
            true,
            null,
            token,
            expiresAtUtc,
            user.Id.ToString(),
            user.Email,
            roles);
    }
}
