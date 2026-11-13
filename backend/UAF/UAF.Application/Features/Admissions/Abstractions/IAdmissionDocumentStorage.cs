namespace UAF.Application.Features.Admissions.Abstractions;

/// <summary>Stores private admission files outside the public web root.</summary>
public interface IAdmissionDocumentStorage
{
    Task<string> SaveAsync(Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteIfExistsAsync(string storageKey, CancellationToken ct = default);
}
