using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class FeeChallanLine : Entity
{
    private FeeChallanLine() { FeeHeadCode = FeeHeadName = string.Empty; }
    public FeeChallanLine(Guid challanId, Guid feeHeadId, string feeHeadCode, string feeHeadName, decimal amount)
    {
        if (challanId == Guid.Empty || feeHeadId == Guid.Empty) throw new ArgumentException("Challan and fee head are required.");
        if (amount <= 0 || decimal.Round(amount,2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        ChallanId = challanId; FeeHeadId = feeHeadId; FeeHeadCode = feeHeadCode; FeeHeadName = feeHeadName; Amount = amount;
    }
    public Guid ChallanId { get; private set; }
    public Guid FeeHeadId { get; private set; }
    public string FeeHeadCode { get; private set; }
    public string FeeHeadName { get; private set; }
    public decimal Amount { get; private set; }
}