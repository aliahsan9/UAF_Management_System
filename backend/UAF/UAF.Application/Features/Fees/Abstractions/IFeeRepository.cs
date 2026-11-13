using UAF.Domain.Entities;
namespace UAF.Application.Features.Fees.Abstractions;
public interface IFeeRepository
{
 Task<bool> ProgramIsActiveAsync(Guid id,CancellationToken ct);
 Task<bool> SessionExistsAsync(Guid id,CancellationToken ct);
 Task<bool> FeeHeadExistsAsync(Guid id,CancellationToken ct);
 Task AddFeeHeadAsync(FeeHead head,CancellationToken ct);
 Task AddStructureAsync(FeeStructure structure,IReadOnlyCollection<FeeStructureItem> items,CancellationToken ct);
 Task PublishStructureAsync(Guid id,CancellationToken ct);
 Task<Guid> GenerateChallanAsync(Guid studentId,Guid structureId,DateOnly issueDate,DateOnly dueDate,decimal lateFee,Guid actorId,CancellationToken ct);
 Task<Guid> RecordPaymentAsync(Guid challanId,decimal amount,string method,string reference,Guid actorId,CancellationToken ct);
 Task VerifyPaymentAsync(Guid paymentId,Guid actorId,bool approve,string? remarks,CancellationToken ct);
 Task<IReadOnlyList<FeeChallan>> GetStudentChallansAsync(Guid studentId,CancellationToken ct);
 Task<IReadOnlyList<FeePayment>> GetPaymentsAsync(Guid studentId,CancellationToken ct);
 Task<decimal> GetOutstandingBalanceAsync(Guid studentId,CancellationToken ct);
}