using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// Durable response reservation for a client-supplied idempotency key.
/// Keys are scoped to an authenticated actor and HTTP operation.
/// </summary>
public sealed class IdempotencyRecord : Entity
{
    private IdempotencyRecord()
    {
        ActorKey = string.Empty;
        OperationKey = string.Empty;
        RequestKey = string.Empty;
        RequestHash = string.Empty;
    }

    public IdempotencyRecord(string actorKey, string operationKey, string requestKey, string requestHash, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(actorKey) || actorKey.Length > 200)
            throw new ArgumentException("A valid idempotency actor key is required.", nameof(actorKey));
        if (string.IsNullOrWhiteSpace(operationKey) || operationKey.Length != 64)
            throw new ArgumentException("A SHA-256 operation fingerprint is required.", nameof(operationKey));
        if (string.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 128)
            throw new ArgumentException("An idempotency key of 1 to 128 characters is required.", nameof(requestKey));
        if (string.IsNullOrWhiteSpace(requestHash) || requestHash.Length != 64)
            throw new ArgumentException("A SHA-256 request fingerprint is required.", nameof(requestHash));
        if (expiresAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));

        ActorKey = actorKey;
        OperationKey = operationKey;
        RequestKey = requestKey;
        RequestHash = requestHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string ActorKey { get; private set; }
    public string OperationKey { get; private set; }
    public string RequestKey { get; private set; }
    public string RequestHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsReplayable { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseContentType { get; private set; }
    public string? ResponseBody { get; private set; }

    public bool HasExpired(DateTimeOffset nowUtc) => ExpiresAtUtc <= nowUtc;

    public void Complete(int statusCode, string? contentType, string responseBody, bool replayable)
    {
        if (IsCompleted) throw new InvalidOperationException("An idempotency record can only be completed once.");
        ResponseStatusCode = statusCode;
        ResponseContentType = contentType;
        ResponseBody = replayable ? responseBody : null;
        IsReplayable = replayable;
        IsCompleted = true;
    }
}
