using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UAF.API.Middleware;

/// <summary>
/// Converts known application exceptions into RFC 9457 Problem Details responses.
/// Unexpected exceptions remain generic and are logged without exposing internals.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) =>
        _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            var validationProblem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://www.rfc-editor.org/rfc/rfc9110#name-400-bad-request",
                Instance = httpContext.Request.Path
            };
            validationProblem.Extensions["traceId"] = httpContext.TraceIdentifier;
            validationProblem.Extensions["correlationId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = validationProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(
                validationProblem, cancellationToken: cancellationToken);
            return true;
        }

        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Request conflicts with the current resource state"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was changed by another request. Reload and retry."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. Path: {Path}, CorrelationId: {CorrelationId}",
                httpContext.Request.Path, httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogInformation(exception, "Request rejected. StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                statusCode, httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://www.rfc-editor.org/rfc/rfc9110#status.{statusCode}",
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "The server could not complete the request."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problem, cancellationToken: cancellationToken);

        return true;
    }
}