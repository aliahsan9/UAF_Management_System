using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Application.Features.Authentication.Models;

namespace UAF.API.Endpoints;

public static class AuthenticationEndpoints
{
    private const string RefreshCookieName = "__Host-uaf-refresh";
    private const string RefreshRequestHeader = "X-Refresh-Request";

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request, IAuthenticationService authentication,
            HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Full name, email and password are required."]
                });

            var result = await authentication.RegisterAsync(request, http.Connection.RemoteIpAddress?.ToString(), ct);
            if (!result.Succeeded) return Results.BadRequest(new { error = result.Error });
            SetRefreshCookie(http, result.RefreshToken!, result.RefreshTokenExpiresAtUtc!.Value);
            return Results.Ok(ToPublicResponse(result));
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("RegisterAccount");

        group.MapPost("/login", async (
            LoginRequest request, IAuthenticationService authentication,
            HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Email and password are required."]
                });

            var result = await authentication.LoginAsync(request, http.Connection.RemoteIpAddress?.ToString(), ct);
            if (!result.Succeeded) return Results.Unauthorized();
            SetRefreshCookie(http, result.RefreshToken!, result.RefreshTokenExpiresAtUtc!.Value);
            return Results.Ok(ToPublicResponse(result));
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("Login");

        group.MapPost("/refresh", async (
            IAuthenticationService authentication, HttpContext http, CancellationToken ct) =>
        {
            if (!HasRefreshRequestHeader(http) ||
                !http.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
                return Results.Unauthorized();

            var result = await authentication.RefreshAsync(
                refreshToken, http.Connection.RemoteIpAddress?.ToString(), ct);
            if (!result.Succeeded)
            {
                ClearRefreshCookie(http);
                return Results.Unauthorized();
            }

            SetRefreshCookie(http, result.RefreshToken!, result.RefreshTokenExpiresAtUtc!.Value);
            return Results.Ok(ToPublicResponse(result));
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("RefreshAccessToken");

        group.MapPost("/revoke", async (
            IAuthenticationService authentication, HttpContext http, CancellationToken ct) =>
        {
            if (!HasRefreshRequestHeader(http) ||
                !http.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
                return Results.BadRequest(new { error = "Refresh credential is missing." });

            await authentication.RevokeAsync(
                refreshToken, http.Connection.RemoteIpAddress?.ToString(), ct);
            ClearRefreshCookie(http);
            return Results.NoContent();
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("RevokeRefreshToken");

        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            FullName = user.FindFirstValue(ClaimTypes.Name),
            Email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"),
            Roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
        }))
        .RequireAuthorization()
        .WithName("GetCurrentUser");

        return endpoints;
    }

    private static bool HasRefreshRequestHeader(HttpContext http) =>
        http.Request.Headers.TryGetValue(RefreshRequestHeader, out var value) && value == "1";

    private static void SetRefreshCookie(HttpContext http, string token, DateTimeOffset expiresAtUtc)
    {
        http.Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = expiresAtUtc,
            IsEssential = true
        });
    }

    private static void ClearRefreshCookie(HttpContext http) =>
        http.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/"
        });

    private static object ToPublicResponse(AuthenticationResult result) => new
    {
        result.AccessToken,
        result.ExpiresAtUtc,
        result.UserId,
        result.Email,
        result.Roles
    };
}
