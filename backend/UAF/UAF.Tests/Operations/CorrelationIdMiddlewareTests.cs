using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using UAF.API.Middleware;
using Xunit;

namespace UAF.Tests.Operations;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesValidCorrelationHeader_AndReturnsIt()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "expo-request_123.abc";
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal("expo-request_123.abc",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal("expo-request_123.abc", context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_ReplacesOversizedCorrelationHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = new string('x', 65);
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
        Assert.DoesNotContain(new string('x', 65), correlationId);
    }
}
