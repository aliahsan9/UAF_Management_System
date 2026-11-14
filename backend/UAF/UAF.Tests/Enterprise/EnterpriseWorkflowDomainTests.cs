using UAF.Application.Features.Workflows;using UAF.Domain.Entities;using UAF.Domain.Enums;using Xunit;
namespace UAF.Tests.Enterprise;
public sealed class EnterpriseWorkflowDomainTests{
 [Fact]public void InAppNotification_CanBeReadOnlyOnce(){var n=new Notification(Guid.NewGuid(),"Welcome","Your account is ready.",NotificationChannel.InApp);Assert.Equal(NotificationStatus.Sent,n.Status);n.MarkRead(DateTimeOffset.UtcNow);var read=n.ReadAtUtc;n.MarkRead(DateTimeOffset.UtcNow.AddMinutes(1));Assert.Equal(NotificationStatus.Read,n.Status);Assert.Equal(read,n.ReadAtUtc);}
 [Fact]public void ExternalNotification_RecordsDeliveryFailure(){var n=new Notification(Guid.NewGuid(),"Fee due","Your fee is due.",NotificationChannel.Email);n.MarkFailed("SMTP unavailable");Assert.Equal(NotificationStatus.Failed,n.Status);Assert.Equal("SMTP unavailable",n.FailureReason);}
 [Fact]public void Workflow_AdvancesSequentiallyAndCompletesAtFinalStage(){var w=new WorkflowInstance("Admission",Guid.NewGuid(),Guid.NewGuid(),2);var now=DateTimeOffset.UtcNow;w.Advance(now);Assert.Equal(2,w.CurrentStage);Assert.Equal(WorkflowStatus.Pending,w.Status);w.Advance(now.AddMinutes(1));Assert.Equal(WorkflowStatus.Approved,w.Status);Assert.NotNull(w.CompletedAtUtc);}
 [Fact]public void WorkflowStep_RequiresConfiguredRoleAndRejectionReason(){var step=new WorkflowApprovalStep(Guid.NewGuid(),1,"Registrar");Assert.Throws<UnauthorizedAccessException>(()=>step.Decide(Guid.NewGuid(),"Teacher",true,null,DateTimeOffset.UtcNow));Assert.Throws<ArgumentException>(()=>step.Decide(Guid.NewGuid(),"Registrar",false,null,DateTimeOffset.UtcNow));}
 [Fact]public void WorkflowStep_EnforcesAssignedApprover(){var actor=Guid.NewGuid();var step=new WorkflowApprovalStep(Guid.NewGuid(),1,"Registrar",actor);Assert.Throws<UnauthorizedAccessException>(()=>step.Decide(Guid.NewGuid(),"Registrar",true,null,DateTimeOffset.UtcNow));step.Decide(actor,"Registrar",true,null,DateTimeOffset.UtcNow);Assert.Equal(WorkflowStepStatus.Approved,step.Status);}
 [Fact]public async Task Decision_UsesTheMatchingRoleWhenTheAccountHasSeveralRoles(){var actor=Guid.NewGuid();var workflow=new WorkflowInstance("Leave",Guid.NewGuid(),Guid.NewGuid(),1);var step=new WorkflowApprovalStep(workflow.Id,1,"Registrar");var service=new ApprovalWorkflowService(new SingleStepWorkflowRepository(workflow,step));await service.DecideAsync(workflow.Id,actor,["Teacher","Registrar"],true,null,CancellationToken.None);Assert.Equal(WorkflowStepStatus.Approved,step.Status);Assert.Equal(WorkflowStatus.Approved,workflow.Status);}
 [Fact]public async Task Decision_RejectsWhenNoneOfTheAccountRolesMatchTheStage(){var workflow=new WorkflowInstance("Leave",Guid.NewGuid(),Guid.NewGuid(),1);var step=new WorkflowApprovalStep(workflow.Id,1,"Registrar");var service=new ApprovalWorkflowService(new SingleStepWorkflowRepository(workflow,step));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.DecideAsync(workflow.Id,Guid.NewGuid(),["Teacher"],true,null,CancellationToken.None));}
 private sealed class SingleStepWorkflowRepository:UAF.Application.Features.Workflows.IWorkflowRepository{
  private readonly WorkflowInstance _workflow;private readonly WorkflowApprovalStep _step;
  public SingleStepWorkflowRepository(WorkflowInstance workflow,WorkflowApprovalStep step){_workflow=workflow;_step=step;}
  public Task<bool> HasActiveAsync(string type,Guid resource,CancellationToken ct)=>Task.FromResult(false);
  public Task AddAsync(WorkflowInstance workflow,IReadOnlyList<WorkflowApprovalStep> steps,WorkflowHistory history,CancellationToken ct)=>Task.CompletedTask;
  public Task<(WorkflowInstance Workflow,WorkflowApprovalStep Step)?> GetCurrentAsync(Guid id,CancellationToken ct)=>Task.FromResult<(WorkflowInstance,WorkflowApprovalStep)?>((_workflow,_step));
  public Task AddHistoryAsync(WorkflowHistory history,CancellationToken ct)=>Task.CompletedTask;
  public Task SaveChangesAsync(CancellationToken ct)=>Task.CompletedTask;
  public Task<IReadOnlyList<WorkflowHistory>> HistoryAsync(Guid id,CancellationToken ct)=>Task.FromResult<IReadOnlyList<WorkflowHistory>>([]);
 }
}
