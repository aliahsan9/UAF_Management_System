using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Transcripts;
namespace UAF.API.Endpoints;
public static class TranscriptEndpoints
{
    public static IEndpointRouteBuilder MapTranscriptEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var issuance = endpoints.MapGroup("/api/v1/transcripts")
            .WithTags("Official Academic Transcripts")
            .RequireAuthorization(AuthorizationPolicies.TranscriptIssuance);
        issuance.MapPost("/students/{studentId:guid}/issue", async (Guid studentId, HttpContext http, TranscriptService service, CancellationToken ct) =>
        {
            var result = await service.IssueAsync(studentId, GetActorId(http.User), ct);
            return Results.Created($"/api/v1/transcripts/verify/{result.VerificationCode}", result);
        }).WithName("IssueOfficialAcademicTranscript");

        var student = endpoints.MapGroup("/api/v1/academic-records/me/transcripts")
            .WithTags("Student Transcripts")
            .RequireAuthorization(AuthorizationPolicies.StudentAcademicRecord);
        student.MapGet("/", async (HttpContext http, TranscriptService service, CancellationToken ct) =>
            Results.Ok(await service.GetMineAsync(GetActorId(http.User), ct)))
            .WithName("GetMyIssuedTranscripts");
        student.MapGet("/{transcriptId:guid}/pdf", async (Guid transcriptId, HttpContext http, TranscriptService service, CancellationToken ct) =>
        {
            var pdf = await service.GetMyPdfAsync(transcriptId, GetActorId(http.User), ct);
            return Results.File(pdf, "application/pdf", $"academic-transcript-{transcriptId:N}.pdf");
        }).WithName("DownloadMyAcademicTranscriptPdf");

        endpoints.MapGet("/api/v1/transcripts/verify/{verificationCode}", async (string verificationCode, TranscriptService service, CancellationToken ct) =>
            Results.Ok(await service.VerifyAsync(verificationCode, ct)))
            .AllowAnonymous()
            .WithTags("Public Transcript Verification")
            .WithName("VerifyAcademicTranscript");
        return endpoints;
    }
    private static Guid GetActorId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id
            : throw new UnauthorizedAccessException("The authenticated account identifier is missing or invalid.");
    }
}
