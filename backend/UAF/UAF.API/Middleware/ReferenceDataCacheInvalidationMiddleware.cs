using UAF.Application.Common.Caching;
namespace UAF.API.Middleware;
public sealed class ReferenceDataCacheInvalidationMiddleware(
    RequestDelegate next, ILogger<ReferenceDataCacheInvalidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IApplicationCache cache)
    {
        await next(context);
        var isWrite = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
                      HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method);
        var isReferenceData = context.Request.Path.StartsWithSegments("/api/v1/academic-catalog") ||
                              context.Request.Path.StartsWithSegments("/api/v1/academic-structure");
        if (isWrite && isReferenceData && context.Response.StatusCode is >= 200 and < 300)
        {
            await cache.RemoveByTagAsync("academic-reference-data", CancellationToken.None);
            logger.LogDebug("Invalidated academic reference-data cache after {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
    }
}
