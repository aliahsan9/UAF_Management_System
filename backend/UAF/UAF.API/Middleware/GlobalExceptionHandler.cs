using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UAF.API.Middleware;

/// <summary>
/// Converts known application exceptions into consistent Problem Details responses.
/// Unexpected exceptions remain generic and are logged without exposing internals.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Request conflicts with the current resource state"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred while processing {Path}.",
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation(exception, "Request rejected with status code {StatusCode}.", statusCode);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "The server could not complete the request."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
