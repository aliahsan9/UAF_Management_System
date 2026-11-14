using Microsoft.EntityFrameworkCore;using UAF.Application.Features.Notifications;using UAF.Application.Features.Workflows;using UAF.Domain.Entities;using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
public sealed class NotificationRepository:INotificationRepository{
 private readonly UafDbContext _db;public NotificationRepository(UafDbContext db)=>_db=db;
 public Task AddAsync(Notification n,CancellationToken ct)=>_db.Notifications.AddAsync(n,ct).AsTask();
 public async Task<IReadOnlyList<Notification>> GetInboxAsync(Guid user,int page,int size,CancellationToken ct)=>await _db.Notifications.AsNoTracking().Where(x=>x.RecipientUserId==user&&x.Channel==NotificationChannel.InApp).OrderByDescending(x=>x.CreatedAtUtc).Skip((page-1)*size).Take(size).ToListAsync(ct);
 public Task<Notification?> GetAsync(Guid id,Guid user,CancellationToken ct)=>_db.Notifications.SingleOrDefaultAsync(x=>x.Id==id&&x.RecipientUserId==user&&x.Channel==NotificationChannel.InApp,ct);
 public Task SaveChangesAsync(CancellationToken ct)=>_db.SaveChangesAsync(ct);
}
public sealed class WorkflowRepository:IWorkflowRepository{
 private readonly UafDbContext _db;public WorkflowRepository(UafDbContext db)=>_db=db;
 public Task<bool> HasActiveAsync(string type,Guid resource,CancellationToken ct)=>_db.WorkflowInstances.AnyAsync(x=>x.ResourceType==type&&x.ResourceId==resource&&x.Status==WorkflowStatus.Pending,ct);
 public async Task AddAsync(WorkflowInstance w,IReadOnlyList<WorkflowApprovalStep> steps,WorkflowHistory h,CancellationToken ct){_db.WorkflowInstances.Add(w);_db.WorkflowApprovalSteps.AddRange(steps);_db.WorkflowHistory.Add(h);await _db.SaveChangesAsync(ct);}
 public async Task<(WorkflowInstance Workflow,WorkflowApprovalStep Step)?> GetCurrentAsync(Guid id,CancellationToken ct){var w=await _db.WorkflowInstances.SingleOrDefaultAsync(x=>x.Id==id,ct);if(w is null)return null;var s=await _db.WorkflowApprovalSteps.SingleOrDefaultAsync(x=>x.WorkflowInstanceId==id&&x.Sequence==w.CurrentStage,ct);return s is null?null:(w,s);}
 public Task AddHistoryAsync(WorkflowHistory h,CancellationToken ct)=>_db.WorkflowHistory.AddAsync(h,ct).AsTask();
 public Task SaveChangesAsync(CancellationToken ct)=>_db.SaveChangesAsync(ct);
 public async Task<IReadOnlyList<WorkflowHistory>> HistoryAsync(Guid id,CancellationToken ct)=>await _db.WorkflowHistory.AsNoTracking().Where(x=>x.WorkflowInstanceId==id).OrderBy(x=>x.OccurredAtUtc).ToListAsync(ct);
}
