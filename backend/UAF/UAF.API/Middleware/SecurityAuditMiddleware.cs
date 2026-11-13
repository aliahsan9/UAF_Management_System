using System.Security.Claims;

namespace UAF.API.Middleware;

/// <summary>
/// Emits structured security audit events for successful and failed state-changing HTTP actions.
/// Request bodies, credentials, query strings and uploaded content are deliberately excluded.
/// </summary>
public sealed class SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isMutation = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
                         HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method);
        if (!isMutation || context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var started = DateTimeOffset.UtcNow;
        try
        {
            await next(context);
        }
        catch
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
            var roles = context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray();
            logger.LogInformation(
                "Security audit event. ActorId: {ActorId}, Roles: {Roles}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, RemoteIp: {RemoteIp}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                userId ?? "anonymous", roles, context.Request.Method, context.Request.Path.Value,
                context.Response.StatusCode, context.Connection.RemoteIpAddress?.ToString(),
                context.TraceIdentifier, (DateTimeOffset.UtcNow - started).TotalMilliseconds);
        }
    }
}
