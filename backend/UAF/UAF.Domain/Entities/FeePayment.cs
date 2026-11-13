using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
public sealed class FeePayment : AuditableEntity
{
    private FeePayment() { ReferenceNumber = Method = string.Empty; }
    public FeePayment(Guid challanId, decimal amount, string method, string referenceNumber, Guid recordedByUserId)
    {
        if (challanId == Guid.Empty || recordedByUserId == Guid.Empty) throw new ArgumentException("Challan and finance officer are required.");
        if (amount <= 0 || decimal.Round(amount,2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount; ChallanId = challanId; Method = Required(method,30); ReferenceNumber = Required(referenceNumber,100);
        RecordedByUserId = recordedByUserId; Status = FeePaymentStatus.PendingVerification;
    }
    public Guid ChallanId { get; private set; }
    public decimal Amount { get; private set; }
    public string Method { get; private set; }
    public string ReferenceNumber { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public string? VerificationRemarks { get; private set; }
    public FeePaymentStatus Status { get; private set; }
    public void Verify(Guid actorId, DateTimeOffset at, string? remarks)
    {
        if (Status != FeePaymentStatus.PendingVerification) throw new InvalidOperationException("Only pending payments can be verified.");
        if (actorId == Guid.Empty || actorId == RecordedByUserId) throw new InvalidOperationException("A different authorized officer must verify the payment.");
        VerifiedByUserId = actorId; VerifiedAtUtc = at.ToUniversalTime(); VerificationRemarks = Normalize(remarks); Status = FeePaymentStatus.Verified; MarkUpdated();
    }
    public void Reject(Guid actorId, DateTimeOffset at, string remarks)
    {
        if (Status != FeePaymentStatus.PendingVerification) throw new InvalidOperationException("Only pending payments can be rejected.");
        if (actorId == Guid.Empty || actorId == RecordedByUserId) throw new InvalidOperationException("A different authorized officer must review the payment.");
        if (string.IsNullOrWhiteSpace(remarks)) throw new ArgumentException("Rejection remarks are required.", nameof(remarks));
        VerifiedByUserId = actorId; VerifiedAtUtc = at.ToUniversalTime(); VerificationRemarks = Normalize(remarks); Status = FeePaymentStatus.Rejected; MarkUpdated();
    }
    private static string Required(string v,int max) { if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max) throw new ArgumentException("A valid value is required."); return v.Trim(); }
    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}