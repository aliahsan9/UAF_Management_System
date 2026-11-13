using Microsoft.AspNetCore.Http;
using UAF.Domain.Enums;

namespace UAF.API.Endpoints;

public sealed class AdmissionDocumentUploadForm
{
    public IFormFile File { get; set; } = null!;
    public AdmissionDocumentType DocumentType { get; set; }
}
