using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Application.Features.Authentication.Models;
using UAF.Infrastructure.Persistence;

namespace UAF.Infrastructure.Identity;

public sealed class IdentityAuthenticationService : IAuthenticationService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
    private const string StudentRole = "Student";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly UafDbContext _dbContext;

    public IdentityAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator tokenGenerator,
        UafDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGenerator = tokenGenerator;
        _dbContext = dbContext;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request, string? remoteIpAddress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return AuthenticationResult.Failure("An account with this email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = email, Email = email,
            FullName = request.FullName.Trim(), EmailConfirmed = false, IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return AuthenticationResult.Failure(string.Join(" ", createResult.Errors.Select(error => error.Description)));

        // Public registration must never be able to select an elevated role.
        var roleResult = await _userManager.AddToRoleAsync(user, StudentRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return AuthenticationResult.Failure("The account could not be assigned its default role.");
        }

        return await CreateAuthenticationResultAsync(user, null, cancellationToken, remoteIpAddress: remoteIpAddress);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, string? remoteIpAddress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        // Keep failures indistinguishable to prevent account enumeration.
        if (user is null || !user.IsActive)
            return AuthenticationResult.Failure("Invalid email or password.");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return AuthenticationResult.Failure("Invalid email or password.");

        return await CreateAuthenticationResultAsync(user, null, cancellationToken, remoteIpAddress: remoteIpAddress);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken, string? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 512)
            return AuthenticationResult.Failure("Invalid refresh token.");

        var tokenHash = HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
            return AuthenticationResult.Failure("Invalid refresh token.");

        if (stored.RevokedAtUtc is not null)
        {
            // Reuse of a rotated token is treated as credential theft: revoke the user's active sessions.
            var activeTokens = await _dbContext.RefreshTokens
                .Where(x => x.UserId == stored.UserId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var active in activeTokens)
                active.Revoke(DateTimeOffset.UtcNow, remoteIpAddress, null);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AuthenticationResult.Failure("Refresh token reuse detected. Please sign in again.");
        }

        if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            return AuthenticationResult.Failure("Refresh token has expired.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            stored.Revoke(DateTimeOffset.UtcNow, remoteIpAddress, null);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AuthenticationResult.Failure("The account is unavailable.");
        }

        var (newRawToken, newToken) = CreateRefreshToken(user.Id, remoteIpAddress);
        stored.Revoke(DateTimeOffset.UtcNow, remoteIpAddress, newToken.TokenHash);
        _dbContext.RefreshTokens.Add(newToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationResult.Failure("Refresh token was already used. Please sign in again.");
        }

        return await CreateAuthenticationResultAsync(user, newRawToken, cancellationToken, persistRefreshToken: false);
    }

    public async Task<bool> RevokeAsync(
        Guid userId, string refreshToken, string? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 512)
            return false;

        var hash = HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
            x => x.TokenHash == hash && x.UserId == userId, cancellationToken);
        if (stored is null || stored.RevokedAtUtc is not null)
            return false;

        stored.Revoke(DateTimeOffset.UtcNow, remoteIpAddress, null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthenticationResult> CreateAuthenticationResultAsync(
        ApplicationUser user, string? alreadyCreatedRefreshToken, CancellationToken ct, bool persistRefreshToken = true, string? remoteIpAddress = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpires) = _tokenGenerator.Generate(user, roles);
        string? rawRefreshToken = alreadyCreatedRefreshToken;
        DateTimeOffset? refreshExpires = null;

        if (persistRefreshToken)
        {
            var created = CreateRefreshToken(user.Id, remoteIpAddress);
            rawRefreshToken = created.RawToken;
            refreshExpires = created.Entity.ExpiresAtUtc;
            _dbContext.RefreshTokens.Add(created.Entity);
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            refreshExpires = await _dbContext.RefreshTokens
                .Where(x => x.TokenHash == HashToken(rawRefreshToken!))
                .Select(x => (DateTimeOffset?)x.ExpiresAtUtc)
                .SingleAsync(ct);
        }

        return new AuthenticationResult(true, null, accessToken, accessExpires,
            user.Id.ToString(), user.Email, roles.ToArray(), rawRefreshToken, refreshExpires);
    }

    private static (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, string? ipAddress)
    {
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var now = DateTimeOffset.UtcNow;
        return (raw, new RefreshToken(userId, HashToken(raw), now, now.Add(RefreshTokenLifetime), ipAddress));
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
