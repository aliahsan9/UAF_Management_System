using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UAF.Domain.Entities;
using UAF.Infrastructure.Persistence;

namespace UAF.API.Middleware;

/// <summary>
/// Replays completed POST responses for a stable Idempotency-Key. The reservation is
/// committed before invoking the endpoint, so concurrent duplicates cannot execute twice.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const long MaximumRequestBytes = 2 * 1024 * 1024;
    private const int MaximumStoredResponseBytes = 1024 * 1024;
    private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(24);
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UafDbContext db)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader))
        {
            await _next(context);
            return;
        }

        var requestKey = keyHeader.ToString().Trim();
        if (requestKey.Length is < 1 or > 128 || requestKey.Any(char.IsControl))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { title = "Idempotency-Key must contain 1 to 128 printable characters." });
            return;
        }

        context.Request.EnableBuffering();
        if (context.Request.ContentLength > MaximumRequestBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        string requestBody;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                   bufferSize: 4096, leaveOpen: true))
            requestBody = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        if (Encoding.UTF8.GetByteCount(requestBody) > MaximumRequestBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var actorKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? context.User.FindFirstValue("sub")
                       ?? $"anonymous:{context.Connection.RemoteIpAddress}";
        var operation = $"{context.Request.Method}:{context.Request.Path}{context.Request.QueryString}";
        var operationKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(operation)));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{operation}\n{requestBody}")));
        var now = DateTimeOffset.UtcNow;

        var existing = await db.IdempotencyRecords.SingleOrDefaultAsync(
            x => x.ActorKey == actorKey && x.OperationKey == operationKey && x.RequestKey == requestKey, context.RequestAborted);

        if (existing is not null && existing.HasExpired(now))
        {
            await db.IdempotencyRecords
                .Where(record => record.Id == existing.Id)
                .ExecuteDeleteAsync(context.RequestAborted);
            db.Entry(existing).State = EntityState.Detached;
            existing = null;
        }

        if (existing is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(existing.RequestHash), Convert.FromHexString(fingerprint)))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new { title = "This idempotency key was already used with a different request." });
                return;
            }

            if (!existing.IsCompleted)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.Headers["Retry-After"] = "2";
                await context.Response.WriteAsJsonAsync(new { title = "A request with this idempotency key is already being processed." });
                return;
            }

            if (!existing.IsReplayable)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new { title = "The original response was too large to replay. Do not repeat the operation." });
                return;
            }

            context.Response.StatusCode = existing.ResponseStatusCode ?? StatusCodes.Status200OK;
            if (!string.IsNullOrWhiteSpace(existing.ResponseContentType))
                context.Response.ContentType = existing.ResponseContentType;
            context.Response.Headers["Idempotency-Replayed"] = "true";
            await context.Response.WriteAsync(existing.ResponseBody ?? string.Empty, context.RequestAborted);
            return;
        }

        var record = new IdempotencyRecord(actorKey, operationKey, requestKey, fingerprint, now.Add(KeyLifetime));
        db.IdempotencyRecords.Add(record);
        try
        {
            await db.SaveChangesAsync(context.RequestAborted);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { title = "A request with this idempotency key is already being processed. Retry shortly." });
            return;
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;
        try
        {
            await _next(context);
            responseBuffer.Position = 0;
            var canReplay = responseBuffer.Length <= MaximumStoredResponseBytes;
            var responseText = canReplay
                ? await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(context.RequestAborted)
                : string.Empty;

            record.Complete(context.Response.StatusCode, context.Response.ContentType, responseText, canReplay);
            if (context.Response.StatusCode >= 500)
            {
                await db.IdempotencyRecords
                    .Where(item => item.Id == record.Id)
                    .ExecuteDeleteAsync(context.RequestAborted);
                db.Entry(record).State = EntityState.Detached;
            }
            else
            {
                await db.SaveChangesAsync(context.RequestAborted);
            }

            responseBuffer.Position = 0;
            context.Response.Body = originalBody;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            context.Response.Body = originalBody;
            try
            {
                await db.IdempotencyRecords
                    .Where(item => item.Id == record.Id)
                    .ExecuteDeleteAsync(CancellationToken.None);
                db.Entry(record).State = EntityState.Detached;
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(cleanupException, "Could not release failed idempotency reservation {RecordId}.", record.Id);
            }
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
