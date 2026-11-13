namespace UAF.Infrastructure.Files;

public sealed class AdmissionDocumentStorageOptions
{
    public const string SectionName = "AdmissionDocumentStorage";
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "private-admission-documents");
}
