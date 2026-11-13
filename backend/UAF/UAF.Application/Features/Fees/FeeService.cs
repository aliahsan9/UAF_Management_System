using UAF.Application.Features.Fees.Abstractions;
using UAF.Application.Features.Fees.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Fees;
public sealed class FeeService
{
 private readonly IFeeRepository _repo;
 public FeeService(IFeeRepository repo)=>_repo=repo??throw new ArgumentNullException(nameof(repo));
 public async Task<Guid> CreateFeeHeadAsync(CreateFeeHeadRequest r,CancellationToken ct)
 { ArgumentNullException.ThrowIfNull(r); var e=new FeeHead(r.Code,r.Name,r.IsMandatory); await _repo.AddFeeHeadAsync(e,ct); return e.Id; }
 public async Task<Guid> CreateStructureAsync(CreateFeeStructureRequest r,CancellationToken ct)
 {
  ArgumentNullException.ThrowIfNull(r);
  if(!await _repo.ProgramIsActiveAsync(r.AcademicProgramId,ct)) throw new KeyNotFoundException("Active academic program was not found.");
  if(!await _repo.SessionExistsAsync(r.AcademicSessionId,ct)) throw new KeyNotFoundException("Academic session was not found.");
  if(r.Items is null||r.Items.Count==0||r.Items.Select(x=>x.FeeHeadId).Distinct().Count()!=r.Items.Count) throw new ArgumentException("At least one unique fee head is required.");
  var structure=new FeeStructure(r.AcademicProgramId,r.AcademicSessionId,r.SemesterNumber,r.Name);
  foreach(var item in r.Items) if(!await _repo.FeeHeadExistsAsync(item.FeeHeadId,ct)) throw new KeyNotFoundException("Fee head was not found.");
  // Structure items are attached by repository after the generated structure ID is assigned.
  await _repo.AddStructureAsync(structure,r.Items.Select(x=>new FeeStructureItem(structure.Id,x.FeeHeadId,x.Amount)).ToArray(),ct);
  return structure.Id;
 }
 public Task PublishStructureAsync(Guid id,CancellationToken ct)=>_repo.PublishStructureAsync(Required(id),ct);
 public Task<Guid> GenerateChallanAsync(GenerateFeeChallanRequest r,Guid actor,CancellationToken ct)
 {
  ArgumentNullException.ThrowIfNull(r); if(r.StudentId==Guid.Empty||r.FeeStructureId==Guid.Empty||actor==Guid.Empty)throw new ArgumentException("Student, fee structure and authenticated officer are required.");
  if(r.LateFeeAmount<0)throw new ArgumentOutOfRangeException(nameof(r.LateFeeAmount));
  return _repo.GenerateChallanAsync(r.StudentId,r.FeeStructureId,r.IssueDate,r.DueDate,r.LateFeeAmount,actor,ct);
 }
 public Task<Guid> RecordPaymentAsync(Guid challanId,RecordFeePaymentRequest r,Guid actor,CancellationToken ct)
 { ArgumentNullException.ThrowIfNull(r);if(actor==Guid.Empty)throw new ArgumentException("Authenticated finance officer is required.");return _repo.RecordPaymentAsync(Required(challanId),r.Amount,r.Method,r.ReferenceNumber,actor,ct); }
 public Task VerifyPaymentAsync(Guid id,VerifyFeePaymentRequest r,Guid actor,CancellationToken ct)
 { ArgumentNullException.ThrowIfNull(r);if(actor==Guid.Empty)throw new ArgumentException("Authenticated finance officer is required.");return _repo.VerifyPaymentAsync(Required(id),actor,r.Approve,r.Remarks,ct); }
 public async Task<StudentFeeAccountResponse> GetAccountAsync(Guid studentId,CancellationToken ct)
 {
  Required(studentId); var challans=await _repo.GetStudentChallansAsync(studentId,ct);var payments=await _repo.GetPaymentsAsync(studentId,ct);
  return new(studentId,await _repo.GetOutstandingBalanceAsync(studentId,ct),challans.Select(Map).ToArray(),payments.Select(Map).ToArray());
 }
 private static Guid Required(Guid id)=>id==Guid.Empty?throw new ArgumentException("A valid identifier is required."):id;
 private static FeeChallanResponse Map(FeeChallan x)=>new(x.Id,x.ChallanNumber,x.IssueDate,x.DueDate,x.Subtotal,x.LateFeeAmount,x.TotalAmount,x.CurrencyCode,x.Status.ToString());
 private static FeePaymentResponse Map(FeePayment x)=>new(x.Id,x.ChallanId,x.Amount,x.Method,x.ReferenceNumber,x.Status.ToString(),x.VerifiedByUserId,x.VerifiedAtUtc,x.VerificationRemarks);
}