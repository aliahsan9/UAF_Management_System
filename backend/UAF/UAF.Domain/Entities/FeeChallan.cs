using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
public sealed class FeeChallan : AuditableEntity
{
    private FeeChallan() { ChallanNumber = CurrencyCode = string.Empty; }
    public FeeChallan(Guid studentFeeAccountId, string challanNumber, DateOnly issueDate, DateOnly dueDate, decimal subtotal, decimal lateFeeAmount, string currencyCode, Guid issuedByUserId)
    {
        if (studentFeeAccountId == Guid.Empty || issuedByUserId == Guid.Empty) throw new ArgumentException("Account and issuing officer are required.");
        if (dueDate < issueDate) throw new ArgumentException("Due date cannot precede issue date.");
        if (subtotal <= 0 || lateFeeAmount < 0 || decimal.Round(subtotal,2) != subtotal || decimal.Round(lateFeeAmount,2) != lateFeeAmount) throw new ArgumentOutOfRangeException(nameof(subtotal));
        StudentFeeAccountId = studentFeeAccountId; ChallanNumber = Required(challanNumber, 60); IssueDate = issueDate; DueDate = dueDate;
        Subtotal = subtotal; LateFeeAmount = lateFeeAmount; CurrencyCode = currencyCode; IssuedByUserId = issuedByUserId; Status = FeeChallanStatus.Issued;
    }
    public Guid StudentFeeAccountId { get; private set; }
    public string ChallanNumber { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal LateFeeAmount { get; private set; }
    public decimal TotalAmount => Subtotal + LateFeeAmount;
    public string CurrencyCode { get; private set; }
    public Guid IssuedByUserId { get; private set; }
    public FeeChallanStatus Status { get; private set; }
    public void RefreshPaymentStatus(decimal verifiedPayments, DateOnly today)
    {
        if (Status == FeeChallanStatus.Cancelled) return;
        if (verifiedPayments >= TotalAmount) Status = FeeChallanStatus.Paid;
        else if (verifiedPayments > 0) Status = FeeChallanStatus.PartiallyPaid;
        else if (today > DueDate) Status = FeeChallanStatus.Overdue;
        else Status = FeeChallanStatus.Issued;
        MarkUpdated();
    }
    public void Cancel() { if (Status == FeeChallanStatus.Paid) throw new InvalidOperationException("A paid challan cannot be cancelled."); Status = FeeChallanStatus.Cancelled; MarkUpdated(); }
    private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException("A valid value is required."); return value.Trim(); }
}