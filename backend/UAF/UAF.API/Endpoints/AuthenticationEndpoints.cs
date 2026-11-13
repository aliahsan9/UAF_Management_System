using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Application.Features.Authentication.Models;

namespace UAF.API.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthenticationService authentication,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Full name, email and password are required."]
                });

            var result = await authentication.RegisterAsync(request, cancellationToken);
            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(new { error = result.Error });
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("RegisterAccount");

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthenticationService authentication,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Email and password are required."]
                });

            var result = await authentication.LoginAsync(request, cancellationToken);
            return result.Succeeded
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("Login");

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
}
