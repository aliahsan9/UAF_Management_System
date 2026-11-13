using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UAF.Infrastructure.Identity;

namespace UAF.API.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration[$"{JwtOptions.SectionName}:Issuer"];
        var audience = configuration[$"{JwtOptions.SectionName}:Audience"];
        var signingKey = configuration[$"{JwtOptions.SectionName}:SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey) ||
            Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Configure Jwt:Issuer, Jwt:Audience and a Jwt:SigningKey of at least 32 bytes using user-secrets or environment variables.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization();
        return services;
    }
}
