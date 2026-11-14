using System.Security.Claims;using Microsoft.AspNetCore.Http;using System.Text.Json;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Diagnostics;using UAF.Domain.Common;using UAF.Domain.Entities;
namespace UAF.Infrastructure.Persistence.Interceptors;
/// <summary>Persists domain create/update/delete snapshots atomically with the business change.</summary>
public sealed class UafCentralAuditInterceptor:SaveChangesInterceptor{
 private static readonly HashSet<string> Sensitive=new(StringComparer.OrdinalIgnoreCase){"Password","PasswordHash","SecurityStamp","ConcurrencyStamp","Token","RefreshToken","Secret","ApiKey"};
 private readonly IHttpContextAccessor _http;public UafCentralAuditInterceptor(IHttpContextAccessor http)=>_http=http;
 public override InterceptionResult<int> SavingChanges(DbContextEventData e,InterceptionResult<int> r){Capture(e.Context);return r;}
 public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e,InterceptionResult<int> r,CancellationToken ct=default){Capture(e.Context);return ValueTask.FromResult(r);}
 private void Capture(DbContext? db){if(db is null)return;var entries=db.ChangeTracker.Entries<Entity>().Where(x=>x.Entity is not AuditTrailEntry&&x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();if(entries.Length==0)return;var ctx=_http.HttpContext;Guid? actor=Guid.TryParse(ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
 foreach(var e in entries){var before=e.State is EntityState.Modified or EntityState.Deleted?Snapshot(e,false):null;var after=e.State is EntityState.Added or EntityState.Modified?Snapshot(e,true):null;var action=e.State switch{EntityState.Added=>"Create",EntityState.Modified=>"Update",_=>"Delete"};db.Set<AuditTrailEntry>().Add(new AuditTrailEntry(actor,action,e.Metadata.ClrType.Name,e.Entity.Id,before,after,ctx?.TraceIdentifier,ctx?.Connection.RemoteIpAddress?.ToString(),DateTimeOffset.UtcNow));}}
 private static string Snapshot(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry e,bool current)=>JsonSerializer.Serialize(e.Properties.Where(p=>!Sensitive.Contains(p.Metadata.Name)&&!p.Metadata.IsShadowProperty()).ToDictionary(p=>p.Metadata.Name,p=>current?p.CurrentValue:p.OriginalValue));
}
