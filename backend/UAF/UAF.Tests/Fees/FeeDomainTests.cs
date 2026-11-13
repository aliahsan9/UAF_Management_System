using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Tests.Fees;
public sealed class FeeDomainTests
{
 [Fact] public void Payment_requires_independent_verifier_and_pending_state()
 {
  var recorder=Guid.NewGuid();var verifier=Guid.NewGuid();var payment=new FeePayment(Guid.NewGuid(),100m,"Bank","REF-1",recorder);
  Assert.Throws<InvalidOperationException>(()=>payment.Verify(recorder,DateTimeOffset.UtcNow,null));
  payment.Verify(verifier,DateTimeOffset.UtcNow,"Matched bank receipt");
  Assert.Equal(FeePaymentStatus.Verified,payment.Status);
  Assert.Throws<InvalidOperationException>(()=>payment.Verify(Guid.NewGuid(),DateTimeOffset.UtcNow,null));
 }
 [Fact] public void Rejected_payment_requires_remarks()
 {
  var payment=new FeePayment(Guid.NewGuid(),100m,"Cash","REF-2",Guid.NewGuid());
  Assert.Throws<ArgumentException>(()=>payment.Reject(Guid.NewGuid(),DateTimeOffset.UtcNow," "));
  payment.Reject(Guid.NewGuid(),DateTimeOffset.UtcNow,"Receipt invalid");
  Assert.Equal(FeePaymentStatus.Rejected,payment.Status);
 }
 [Fact] public void Challan_status_reflects_verified_amount_only()
 {
  var c=new FeeChallan(Guid.NewGuid(),"UAF-FEE-TEST",new DateOnly(2026,1,1),new DateOnly(2026,1,15),1000m,100m,"PKR",Guid.NewGuid());
  c.RefreshPaymentStatus(500m,new DateOnly(2026,1,10));
  Assert.Equal(FeeChallanStatus.PartiallyPaid,c.Status);
  c.RefreshPaymentStatus(1100m,new DateOnly(2026,1,10));
  Assert.Equal(FeeChallanStatus.Paid,c.Status);
 }
}