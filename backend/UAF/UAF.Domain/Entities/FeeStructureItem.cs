using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class FeeStructureItem : Entity
{
    private FeeStructureItem() { }
    public FeeStructureItem(Guid feeStructureId, Guid feeHeadId, decimal amount)
    {
        if (feeStructureId == Guid.Empty || feeHeadId == Guid.Empty) throw new ArgumentException("Fee structure and fee head are required.");
        if (amount < 0 || amount > 100000000m || decimal.Round(amount, 2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        FeeStructureId = feeStructureId; FeeHeadId = feeHeadId; Amount = amount;
    }
    public Guid FeeStructureId { get; private set; }
    public Guid FeeHeadId { get; private set; }
    public FeeHead FeeHead { get; private set; } = null!;
    public decimal Amount { get; private set; }
}