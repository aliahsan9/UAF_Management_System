namespace UAF.Application.Common.Abstractions;
/// <summary>Private file storage contract. Providers can be switched from local disk to cloud object storage.</summary>
public interface IFileStorage{Task<string> SaveAsync(Stream content,CancellationToken ct=default);Task<Stream> OpenReadAsync(string storageKey,CancellationToken ct=default);Task DeleteIfExistsAsync(string storageKey,CancellationToken ct=default);}
