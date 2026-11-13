using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>Registrar-configured registration-number sequence for a program and intake year.</summary>
public sealed class RegistrationNumberSeries : AuditableEntity
{
    private RegistrationNumberSeries() { }

    public RegistrationNumberSeries(Guid academicProgramId, int admissionYear, string prefix, int nextNumber, Guid configuredByUserId)
    {
        if (academicProgramId == Guid.Empty) throw new ArgumentException("Academic program is required.", nameof(academicProgramId));
        if (admissionYear < 2000 || admissionYear > 9999) throw new ArgumentOutOfRangeException(nameof(admissionYear));
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Trim().Length > 20 ||
            prefix.Trim().Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new ArgumentException("Prefix must be 1-20 ASCII letters, digits, or hyphens.", nameof(prefix));
        if (nextNumber < 1) throw new ArgumentOutOfRangeException(nameof(nextNumber));
        if (configuredByUserId == Guid.Empty) throw new ArgumentException("Configuring staff account is required.", nameof(configuredByUserId));
        AcademicProgramId = academicProgramId;
        AdmissionYear = admissionYear;
        Prefix = prefix.Trim().ToUpperInvariant();
        NextNumber = nextNumber;
        ConfiguredByUserId = configuredByUserId;
    }

    public Guid AcademicProgramId { get; private set; }
    public int AdmissionYear { get; private set; }
    public string Prefix { get; private set; }
    public int NextNumber { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }

    public string Allocate()
    {
        if (NextNumber == int.MaxValue) throw new InvalidOperationException("Registration number sequence is exhausted.");
        var allocated = $"{Prefix}-{AdmissionYear}-{NextNumber:D5}";
        NextNumber++;
        MarkUpdated();
        return allocated;
    }

    public void Reconfigure(string prefix, int nextNumber, Guid configuredByUserId)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Trim().Length > 20 ||
            prefix.Trim().Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new ArgumentException("Prefix must be 1-20 ASCII letters, digits, or hyphens.", nameof(prefix));
        if (nextNumber < NextNumber) throw new InvalidOperationException("A sequence cannot be moved backwards.");
        if (configuredByUserId == Guid.Empty) throw new ArgumentException("Configuring staff account is required.", nameof(configuredByUserId));
        Prefix = prefix.Trim().ToUpperInvariant();
        NextNumber = nextNumber;
        ConfiguredByUserId = configuredByUserId;
        MarkUpdated();
    }
}
