using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>Program-specific document policy configured by authorized admissions staff.</summary>
public sealed class AdmissionDocumentRequirement : AuditableEntity
{
    private AdmissionDocumentRequirement() { }

    public AdmissionDocumentRequirement(Guid academicProgramId, AdmissionDocumentType documentType, Guid configuredByUserId)
    {
        if (academicProgramId == Guid.Empty) throw new ArgumentException("Academic program is required.", nameof(academicProgramId));
        if (!Enum.IsDefined(documentType)) throw new ArgumentOutOfRangeException(nameof(documentType));
        if (configuredByUserId == Guid.Empty) throw new ArgumentException("Configured-by user is required.", nameof(configuredByUserId));
        AcademicProgramId = academicProgramId;
        DocumentType = documentType;
        ConfiguredByUserId = configuredByUserId;
        IsRequired = true;
    }

    public Guid AcademicProgramId { get; private set; }
    public AdmissionDocumentType DocumentType { get; private set; }
    public bool IsRequired { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }

    public void Reconfigure(Guid configuredByUserId)
    {
        if (configuredByUserId == Guid.Empty) throw new ArgumentException("Configured-by user is required.", nameof(configuredByUserId));
        IsRequired = true;
        ConfiguredByUserId = configuredByUserId;
        MarkUpdated();
    }
}
