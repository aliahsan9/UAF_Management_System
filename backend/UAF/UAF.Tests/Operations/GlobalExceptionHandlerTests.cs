using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UAF.API.Middleware;
using Xunit;

namespace UAF.Tests.Operations;

public sealed class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> KnownExceptions => new()
    {
        { new ArgumentException("invalid field"), StatusCodes.Status400BadRequest, "Invalid request" },
        { new KeyNotFoundException("missing record"), StatusCodes.Status404NotFound, "Resource not found" },
        { new UnauthorizedAccessException("not permitted"), StatusCodes.Status403Forbidden, "Access denied" },
        { new InvalidOperationException("invalid state"), StatusCodes.Status409Conflict, "Request conflicts with the current resource state" },
        { new DbUpdateConcurrencyException("stale row"), StatusCodes.Status409Conflict, "The record was changed by another request. Reload and retry." }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task TryHandleAsync_MapsKnownExceptionsToProblemDetails(
        Exception exception, int expectedStatus, string expectedTitle)
    {
        var context = CreateContext();
        var handler = CreateHandler();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        using var document = await ReadResponseAsync(context);
        var root = document.RootElement;
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, root.GetProperty("title").GetString());
        Assert.Equal("/api/students/123", root.GetProperty("instance").GetString());
        Assert.Equal("correlation-123", root.GetProperty("traceId").GetString());
        Assert.Equal("correlation-123", root.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotExposeUnexpectedExceptionDetails()
    {
        var context = CreateContext();
        var handler = CreateHandler();
        const string sensitiveMessage = "ConnectionString=Server=internal;Password=secret";

        var handled = await handler.TryHandleAsync(
            context, new Exception(sensitiveMessage), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        using var document = await ReadResponseAsync(context);
        var root = document.RootElement;
        Assert.Equal("An unexpected error occurred", root.GetProperty("title").GetString());
        Assert.Equal("The server could not complete the request.", root.GetProperty("detail").GetString());
        Assert.DoesNotContain(sensitiveMessage, root.GetRawText(), StringComparison.Ordinal);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/students/123";
        context.TraceIdentifier = "correlation-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static GlobalExceptionHandler CreateHandler() =>
        new(NullLogger<GlobalExceptionHandler>.Instance);

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
