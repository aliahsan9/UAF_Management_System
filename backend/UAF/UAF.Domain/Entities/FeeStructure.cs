using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class FeeStructure : AuditableEntity
{
    private FeeStructure() { Name = CurrencyCode = "PKR"; }
    public FeeStructure(Guid academicProgramId, Guid academicSessionId, int semesterNumber, string name, string currencyCode = "PKR")
    {
        if (academicProgramId == Guid.Empty || academicSessionId == Guid.Empty) throw new ArgumentException("Program and academic session are required.");
        if (semesterNumber is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(semesterNumber));
        AcademicProgramId = academicProgramId; AcademicSessionId = academicSessionId; SemesterNumber = semesterNumber;
        Name = Required(name, 120); CurrencyCode = Required(currencyCode, 3).ToUpperInvariant();
        if (CurrencyCode != "PKR") throw new ArgumentException("This release supports PKR fee structures only.", nameof(currencyCode));
        IsPublished = false; IsActive = true;
    }
    public Guid AcademicProgramId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public int SemesterNumber { get; private set; }
    public string Name { get; private set; }
    public string CurrencyCode { get; private set; }
    public bool IsPublished { get; private set; }
    public bool IsActive { get; private set; }
    public void Publish() { if (!IsActive) throw new InvalidOperationException("Inactive fee structure cannot be published."); IsPublished = true; MarkUpdated(); }
    public void Deactivate() { IsActive = false; MarkUpdated(); }
    private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException("A valid value is required."); return value.Trim(); }
}