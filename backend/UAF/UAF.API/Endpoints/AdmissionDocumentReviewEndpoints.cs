using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Models;

namespace UAF.API.Endpoints;

/// <summary>Staff-only admission document review and retrieval.</summary>
public static class AdmissionDocumentReviewEndpoints
{
    public static IEndpointRouteBuilder MapAdmissionDocumentReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admissions/documents")
            .WithTags("Admission Document Review")
            .RequireAuthorization(AuthorizationPolicies.AdmissionsStaff);

        group.MapGet("/requirements/program/{academicProgramId:guid}", async (Guid academicProgramId,
            AdmissionDocumentService service, CancellationToken ct) =>
            Results.Ok(await service.GetConfiguredRequirementsAsync(academicProgramId, ct)));

        group.MapPut("/requirements/program/{academicProgramId:guid}", async (Guid academicProgramId,
            ConfigureAdmissionRequirementsRequest request, ClaimsPrincipal user,
            AdmissionDocumentService service, CancellationToken ct) =>
        {
            await service.ConfigureRequirementsAsync(academicProgramId, GetUserId(user), request.RequiredDocumentTypes, ct);
            return Results.NoContent();
        }).RequireAuthorization(AuthorizationPolicies.AdmissionsDecision);

        group.MapGet("/application/{applicationId:guid}", async (Guid applicationId,
            AdmissionDocumentService service, CancellationToken ct) =>
            Results.Ok(await service.GetForStaffAsync(applicationId, ct)));

        group.MapGet("/{documentId:guid}/download", async (Guid documentId,
            AdmissionDocumentService service, HttpContext http, CancellationToken ct) =>
        {
            var download = await service.DownloadForStaffAsync(documentId, ct);
            http.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(download.Content, download.Document.ContentType,
                download.Document.OriginalFileName, enableRangeProcessing: false);
        });

        group.MapPost("/{documentId:guid}/verify", async (Guid documentId,
            VerifyAdmissionDocumentRequest request, ClaimsPrincipal user,
            AdmissionDocumentService service, CancellationToken ct) =>
        {
            await service.VerifyAsync(documentId, GetUserId(user), request.Remarks, ct);
            return Results.NoContent();
        });

        group.MapPost("/{documentId:guid}/reject", async (Guid documentId,
            RejectAdmissionDocumentRequest request, ClaimsPrincipal user,
            AdmissionDocumentService service, CancellationToken ct) =>
        {
            await service.RejectAsync(documentId, GetUserId(user), request.Remarks, ct);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated reviewer identifier is invalid.");
        return userId;
    }
}
