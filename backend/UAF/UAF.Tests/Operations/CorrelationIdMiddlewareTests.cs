using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using UAF.API.Middleware;
using Xunit;

namespace UAF.Tests.Operations;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesValidCorrelationHeader_AndReturnsItImmediately()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "expo-request_123.abc";
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.Equal("expo-request_123.abc", context.TraceIdentifier);
        Assert.Equal("expo-request_123.abc",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid value")]
    [InlineData("bad\r\nheader")]
    public async Task InvokeAsync_ReplacesInvalidCorrelationHeader(string invalidValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = invalidValue;
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        AssertGeneratedCorrelationId(context);
    }

    [Fact]
    public async Task InvokeAsync_ReplacesOversizedCorrelationHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = new string('x', 65);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        AssertGeneratedCorrelationId(context);
    }

    [Fact]
    public async Task InvokeAsync_SetsCorrelationContextBeforeCallingNextMiddleware()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "request-42";
        var nextWasCalled = false;
        var middleware = new CorrelationIdMiddleware(httpContext =>
        {
            nextWasCalled = true;
            Assert.Equal("request-42", httpContext.TraceIdentifier);
            Assert.Equal("request-42",
                httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
            return Task.CompletedTask;
        }, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
    }

    private static CorrelationIdMiddleware CreateMiddleware() =>
        new(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

    private static void AssertGeneratedCorrelationId(HttpContext context)
    {
        var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
        Assert.Equal(correlationId, context.TraceIdentifier);
    }
}
