using UAF.Domain.Entities;using UAF.Domain.Enums;
namespace UAF.Application.Features.Notifications;
public interface INotificationRepository{Task AddAsync(Notification n,CancellationToken ct);Task<IReadOnlyList<Notification>> GetInboxAsync(Guid user,int page,int size,CancellationToken ct);Task<Notification?> GetAsync(Guid id,Guid user,CancellationToken ct);Task SaveChangesAsync(CancellationToken ct);}
public interface INotificationPublisher{Task PublishAsync(Guid user,object payload,CancellationToken ct);}
public interface IEmailSender{Task SendAsync(string to,string subject,string body,CancellationToken ct);}
public interface ISmsSender{Task SendAsync(string to,string message,CancellationToken ct);}
public sealed record SendNotificationRequest(Guid RecipientUserId,string Title,string Message,NotificationChannel Channel,string? Destination);
public sealed record NotificationResponse(Guid Id,string Title,string Message,NotificationChannel Channel,NotificationStatus Status,DateTimeOffset CreatedAtUtc,DateTimeOffset? ReadAtUtc);
public sealed class NotificationService{
 private readonly INotificationRepository _repo;private readonly INotificationPublisher _publisher;private readonly IEmailSender _email;private readonly ISmsSender _sms;
 public NotificationService(INotificationRepository repo,INotificationPublisher publisher,IEmailSender email,ISmsSender sms){_repo=repo;_publisher=publisher;_email=email;_sms=sms;}
 public async Task<Guid> SendAsync(SendNotificationRequest r,CancellationToken ct){ArgumentNullException.ThrowIfNull(r);if(r.Channel!=NotificationChannel.InApp&&string.IsNullOrWhiteSpace(r.Destination))throw new ArgumentException("Destination is required for external delivery.");var n=new Notification(r.RecipientUserId,r.Title,r.Message,r.Channel);await _repo.AddAsync(n,ct);if(r.Channel==NotificationChannel.Email){try{await _email.SendAsync(r.Destination!,r.Title,r.Message,ct);n.MarkSent(DateTimeOffset.UtcNow);}catch(Exception ex)when(ex is not OperationCanceledException){n.MarkFailed(ex.Message);}}else if(r.Channel==NotificationChannel.Sms){try{await _sms.SendAsync(r.Destination!,r.Message,ct);n.MarkSent(DateTimeOffset.UtcNow);}catch(Exception ex)when(ex is not OperationCanceledException){n.MarkFailed(ex.Message);}}await _repo.SaveChangesAsync(ct);if(r.Channel==NotificationChannel.InApp)await _publisher.PublishAsync(r.RecipientUserId,Map(n),ct);return n.Id;}
 public async Task<IReadOnlyList<NotificationResponse>> InboxAsync(Guid user,int page,int size,CancellationToken ct){if(user==Guid.Empty)throw new ArgumentException("User required.");if(page<1||size is <1 or >100)throw new ArgumentOutOfRangeException(nameof(size));return(await _repo.GetInboxAsync(user,page,size,ct)).Select(Map).ToArray();}
 public async Task MarkReadAsync(Guid id,Guid user,CancellationToken ct){var n=await _repo.GetAsync(id,user,ct)??throw new KeyNotFoundException("Notification not found.");n.MarkRead(DateTimeOffset.UtcNow);await _repo.SaveChangesAsync(ct);}
 private static NotificationResponse Map(Notification n)=>new(n.Id,n.Title,n.Message,n.Channel,n.Status,n.CreatedAtUtc,n.ReadAtUtc);
}
