using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Models;

namespace UAF.API.Endpoints;

/// <summary>
/// HTTP endpoints for the admission application workflow.
/// Endpoints perform transport validation and delegate business rules to Application.
/// </summary>
public static class AdmissionsEndpoints
{
    public static IEndpointRouteBuilder MapAdmissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admissions")
            .WithTags("Admissions");

        group.MapPost("/", CreateDraft)
            .WithName("CreateAdmissionDraft")
            .WithSummary("Create an admission application draft")
            .Produces<CreateAdmissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitAdmissionApplication")
            .WithSummary("Submit an admission application")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/start-review", StartReview)
            .WithName("StartAdmissionReview")
            .WithSummary("Move a submitted application into review")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/accept", Accept)
            .WithName("AcceptAdmissionApplication")
            .WithSummary("Accept an admission application")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectAdmissionApplication")
            .WithSummary("Reject an admission application")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/waitlist", Waitlist)
            .WithName("WaitlistAdmissionApplication")
            .WithSummary("Place an admission application on the waitlist")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/withdraw", Withdraw)
            .WithName("WithdrawAdmissionApplication")
            .WithSummary("Withdraw an admission application")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateDraft(
        CreateAdmissionDraftRequest request,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var applicationId = await service.CreateDraftAsync(
            new CreateAdmissionApplicationRequest(
                request.ApplicationNumber.Trim(),
                request.ApplicantFullName.Trim(),
                request.ApplicantEmail.Trim(),
                request.AcademicProgramId,
                request.ApplicationDate,
                request.PhoneNumber?.Trim()),
            cancellationToken);

        var response = new CreateAdmissionResponse(applicationId);

        return Results.Created($"/api/v1/admissions/{applicationId}", response);
    }

    private static async Task<IResult> Submit(
        Guid id,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.SubmitAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> StartReview(
        Guid id,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.StartReviewAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Accept(
        Guid id,
        AdmissionDecisionRequest? request,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.AcceptAsync(id, request?.Remarks, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Reject(
        Guid id,
        AdmissionDecisionRequest? request,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.RejectAsync(id, request?.Remarks, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Waitlist(
        Guid id,
        AdmissionDecisionRequest? request,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.WaitlistAsync(id, request?.Remarks, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Withdraw(
        Guid id,
        AdmissionApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.WithdrawAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static Dictionary<string, string[]> Validate(CreateAdmissionDraftRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ApplicationNumber))
            errors[nameof(request.ApplicationNumber)] = ["Application number is required."];
        else if (request.ApplicationNumber.Trim().Length > 50)
            errors[nameof(request.ApplicationNumber)] = ["Application number cannot exceed 50 characters."];

        if (string.IsNullOrWhiteSpace(request.ApplicantFullName))
            errors[nameof(request.ApplicantFullName)] = ["Applicant name is required."];
        else if (request.ApplicantFullName.Trim().Length > 180)
            errors[nameof(request.ApplicantFullName)] = ["Applicant name cannot exceed 180 characters."];

        if (string.IsNullOrWhiteSpace(request.ApplicantEmail))
            errors[nameof(request.ApplicantEmail)] = ["Applicant email is required."];
        else if (request.ApplicantEmail.Trim().Length > 254 ||
                 !System.Net.Mail.MailAddress.TryCreate(request.ApplicantEmail.Trim(), out var address) ||
                 !string.Equals(address.Address, request.ApplicantEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            errors[nameof(request.ApplicantEmail)] = ["A valid email address is required."];

        if (request.AcademicProgramId == Guid.Empty)
            errors[nameof(request.AcademicProgramId)] = ["A valid academic program ID is required."];

        if (request.ApplicationDate == default)
            errors[nameof(request.ApplicationDate)] = ["Application date is required."];

        if (request.PhoneNumber?.Length > 30)
            errors[nameof(request.PhoneNumber)] = ["Phone number cannot exceed 30 characters."];

        return errors;
    }
}

public sealed record CreateAdmissionDraftRequest(
    string ApplicationNumber,
    string ApplicantFullName,
    string ApplicantEmail,
    Guid AcademicProgramId,
    DateOnly ApplicationDate,
    string? PhoneNumber = null);

public sealed record AdmissionDecisionRequest(string? Remarks);

public sealed record CreateAdmissionResponse(Guid ApplicationId);
