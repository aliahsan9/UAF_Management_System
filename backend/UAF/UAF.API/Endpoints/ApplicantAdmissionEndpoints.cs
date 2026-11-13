using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Models;
using UAF.Domain.Enums;

namespace UAF.API.Endpoints;

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

        group.MapGet("/{id:guid}/documents", async (Guid id, ClaimsPrincipal user,
            AdmissionDocumentService documents, CancellationToken ct) =>
            Results.Ok(await documents.GetMyDocumentsAsync(GetUserId(user), id, ct)));

        group.MapPost("/{id:guid}/documents", async (Guid id, IFormFile file,
            AdmissionDocumentType documentType, ClaimsPrincipal user,
            AdmissionDocumentService documents, CancellationToken ct) =>
        {
            if (file is null || file.Length <= 0)
                return Results.BadRequest(new { message = "A non-empty file is required." });
            if (file.Length > AdmissionDocumentService.MaximumFileSizeBytes)
                return Results.BadRequest(new { message = "File size cannot exceed 10 MB." });

            await using var input = file.OpenReadStream();
            var result = await documents.UploadForApplicantAsync(
                GetUserId(user), id, documentType, file.FileName, file.ContentType, input, ct);
            return Results.Created($"/api/v1/applicant/admissions/{id}/documents/{result.Id}", result);
        })
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data");

        group.MapGet("/{applicationId:guid}/documents/{documentId:guid}/download",
            async (Guid applicationId, Guid documentId, ClaimsPrincipal user,
                AdmissionDocumentService documents, HttpContext http, CancellationToken ct) =>
            {
                var download = await documents.DownloadMyDocumentAsync(
                    GetUserId(user), applicationId, documentId, ct);
                http.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Results.File(download.Content, download.Document.ContentType,
                    download.Document.OriginalFileName, enableRangeProcessing: false);
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
