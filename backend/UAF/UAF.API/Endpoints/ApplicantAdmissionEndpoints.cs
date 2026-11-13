using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Models;

namespace UAF.API.Endpoints;

/// <summary>Authenticated applicant self-service. Every operation is scoped to the JWT subject.</summary>
public static class ApplicantAdmissionEndpoints
{
    public static IEndpointRouteBuilder MapApplicantAdmissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/applicant/admissions")
            .WithTags("Applicant Admissions")
            .RequireAuthorization(AuthorizationPolicies.ApplicantSelfService);

        group.MapGet("/", async (ClaimsPrincipal user, AdmissionApplicationService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyApplicationsAsync(GetUserId(user), ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, AdmissionApplicationService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyApplicationAsync(GetUserId(user), id, ct)));

        group.MapPost("/", async (CreateApplicantDraftRequest request, ClaimsPrincipal user,
            AdmissionApplicationService service, CancellationToken ct) =>
        {
            var result = await service.CreateApplicantDraftAsync(GetUserId(user), request, ct);
            return Results.Created($"/api/v1/applicant/admissions/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateApplicantDraftRequest request, ClaimsPrincipal user,
            AdmissionApplicationService service, CancellationToken ct) =>
        {
            await service.UpdateMyDraftAsync(GetUserId(user), id, request, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/submit", async (Guid id, ClaimsPrincipal user,
            AdmissionApplicationService service, CancellationToken ct) =>
        {
            await service.SubmitMyApplicationAsync(GetUserId(user), id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/withdraw", async (Guid id, ClaimsPrincipal user,
            AdmissionApplicationService service, CancellationToken ct) =>
        {
            await service.WithdrawMyApplicationAsync(GetUserId(user), id, ct);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated account identifier is invalid.");
        return userId;
    }
}
