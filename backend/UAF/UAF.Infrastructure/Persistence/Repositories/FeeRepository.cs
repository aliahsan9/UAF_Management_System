using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Fees.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class FeeRepository : IFeeRepository
{
 private readonly UafDbContext db;
 public FeeRepository(UafDbContext db)=>this.db=db;
 public Task<bool> ProgramIsActiveAsync(Guid id,CancellationToken ct)=>db.AcademicPrograms.AnyAsync(x=>x.Id==id&&x.IsActive,ct);
 public Task<bool> SessionExistsAsync(Guid id,CancellationToken ct)=>db.AcademicSessions.AnyAsync(x=>x.Id==id&&x.Status!=AcademicSessionStatus.Cancelled,ct);
 public Task<bool> FeeHeadExistsAsync(Guid id,CancellationToken ct)=>db.FeeHeads.AnyAsync(x=>x.Id==id&&x.IsActive,ct);
 public async Task AddFeeHeadAsync(FeeHead head,CancellationToken ct){db.FeeHeads.Add(head);await db.SaveChangesAsync(ct);}
 public async Task AddStructureAsync(FeeStructure structure,IReadOnlyCollection<FeeStructureItem> items,CancellationToken ct)
 { await using var tx=await db.Database.BeginTransactionAsync(ct);db.FeeStructures.Add(structure);await db.SaveChangesAsync(ct);foreach(var x in items)db.FeeStructureItems.Add(x);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct); }
 public async Task PublishStructureAsync(Guid id,CancellationToken ct)
 { var x=await db.FeeStructures.Include(s=>s.Items).FirstOrDefaultAsync(s=>s.Id==id,ct)??throw new KeyNotFoundException("Fee structure not found.");if(x.Items.Count==0)throw new InvalidOperationException("A fee structure must contain at least one fee item.");x.Publish();await db.SaveChangesAsync(ct); }
 public async Task<Guid> GenerateChallanAsync(Guid studentId,Guid structureId,DateOnly issue,DateOnly due,decimal lateFee,Guid actor,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var student=await db.Students.FirstOrDefaultAsync(x=>x.Id==studentId,ct)??throw new KeyNotFoundException("Student not found.");
  if(student.Status!=StudentStatus.Active)throw new InvalidOperationException("Only active students can be charged.");
  var structure=await db.FeeStructures.Include(x=>x.Items).ThenInclude(x=>x.FeeHead).FirstOrDefaultAsync(x=>x.Id==structureId&&x.IsPublished&&x.IsActive,ct)??throw new KeyNotFoundException("Published active fee structure not found.");
  if(structure.AcademicProgramId!=student.AcademicProgramId)throw new InvalidOperationException("Fee structure does not belong to the student's current academic program.");
  if(due<issue)throw new ArgumentException("Due date cannot precede issue date.");
  if(lateFee<0||decimal.Round(lateFee,2)!=lateFee)throw new ArgumentOutOfRangeException(nameof(lateFee));
  var account=await db.StudentFeeAccounts.FirstOrDefaultAsync(x=>x.StudentId==studentId,ct);
  if(account is null){account=new StudentFeeAccount(studentId);db.StudentFeeAccounts.Add(account);await db.SaveChangesAsync(ct);}
  var challanNumber=$"UAF-FEE-{issue:yyyyMMdd}-{Guid.NewGuid():N}";
  var lines=structure.Items.Where(x=>x.Amount>0).ToArray();var subtotal=lines.Sum(x=>x.Amount);if(subtotal<=0)throw new InvalidOperationException("Published fee structure has no chargeable items.");
  var challan=new FeeChallan(account.Id,challanNumber,issue,due,subtotal,lateFee,structure.CurrencyCode,actor);
  db.FeeChallans.Add(challan);
  foreach(var line in lines)db.FeeChallanLines.Add(new FeeChallanLine(challan.Id,line.FeeHeadId,line.FeeHead.Code,line.FeeHead.Name,line.Amount));
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return challan.Id;
 }
 public async Task<Guid> RecordPaymentAsync(Guid challanId,decimal amount,string method,string reference,Guid actor,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var challan=await db.FeeChallans.FromSqlInterpolated($"SELECT * FROM [FeeChallans] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {challanId}").FirstOrDefaultAsync(ct)??throw new KeyNotFoundException("Challan not found.");
  if(challan.Status==FeeChallanStatus.Cancelled)throw new InvalidOperationException("Cancelled challan cannot receive payments.");
  var verified=await db.FeePayments.Where(x=>x.ChallanId==challanId&&x.Status==FeePaymentStatus.Verified).SumAsync(x=>(decimal?)x.Amount,ct)??0m;
  if(verified+amount>challan.TotalAmount)throw new InvalidOperationException("Payment exceeds the remaining challan balance.");
  var payment=new FeePayment(challanId,amount,method,reference,actor);db.FeePayments.Add(payment);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return payment.Id;
 }
 public async Task VerifyPaymentAsync(Guid paymentId,Guid actor,bool approve,string? remarks,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var payment=await db.FeePayments.FromSqlInterpolated($"SELECT * FROM [FeePayments] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {paymentId}").FirstOrDefaultAsync(ct)??throw new KeyNotFoundException("Payment not found.");
  var challan=await db.FeeChallans.FromSqlInterpolated($"SELECT * FROM [FeeChallans] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {payment.ChallanId}").FirstAsync(ct);
  if(approve)
  {
   var verified=await db.FeePayments.Where(x=>x.ChallanId==challan.Id&&x.Status==FeePaymentStatus.Verified).SumAsync(x=>(decimal?)x.Amount,ct)??0m;
   if(verified+payment.Amount>challan.TotalAmount)throw new InvalidOperationException("Verification would overpay this challan.");
   payment.Verify(actor,DateTimeOffset.UtcNow,remarks);
  }
  else payment.Reject(actor,DateTimeOffset.UtcNow,remarks??string.Empty);
  var total=await db.FeePayments.Where(x=>x.ChallanId==challan.Id&&x.Status==FeePaymentStatus.Verified).SumAsync(x=>(decimal?)x.Amount,ct)??0m;
  challan.RefreshPaymentStatus(total,DateOnly.FromDateTime(DateTime.UtcNow));
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
 }
 public async Task<IReadOnlyList<FeeChallan>> GetStudentChallansAsync(Guid studentId,CancellationToken ct)=>await db.FeeChallans.AsNoTracking().Where(x=>x.StudentFeeAccount.StudentId==studentId).OrderByDescending(x=>x.IssueDate).ToListAsync(ct);
 public async Task<IReadOnlyList<FeePayment>> GetPaymentsAsync(Guid studentId,CancellationToken ct)=>await db.FeePayments.AsNoTracking().Where(x=>x.Challan.StudentFeeAccount.StudentId==studentId).OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct);
 public async Task<decimal> GetOutstandingBalanceAsync(Guid studentId,CancellationToken ct)
 {
  var query=from c in db.FeeChallans where c.StudentFeeAccount.StudentId==studentId&&c.Status!=FeeChallanStatus.Cancelled
   join p in db.FeePayments.Where(x=>x.Status==FeePaymentStatus.Verified) on c.Id equals p.ChallanId into ps
   select new { c.TotalAmount, Paid=ps.Sum(x=>(decimal?)x.Amount)??0m };
  return await query.SumAsync(x=>(decimal?)(x.TotalAmount-x.Paid),ct)??0m;
 }
}