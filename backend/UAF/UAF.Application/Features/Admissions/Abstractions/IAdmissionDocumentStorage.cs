using UAF.Application.Common.Abstractions;
namespace UAF.Application.Features.Admissions.Abstractions;
/// <summary>Backward-compatible admission-specific alias for the generic private file storage contract.</summary>
public interface IAdmissionDocumentStorage:IFileStorage{}
