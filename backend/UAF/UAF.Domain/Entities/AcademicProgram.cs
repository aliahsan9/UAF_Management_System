using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// A degree program offered by an academic department.
/// </summary>
public sealed class AcademicProgram : AuditableEntity
{
    private AcademicProgram()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public AcademicProgram(
        Guid departmentId,
        string name,
        string code,
        DegreeLevel degreeLevel,
        int durationInYears)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("A department must be specified.", nameof(departmentId));

        if (!Enum.IsDefined(degreeLevel))
            throw new ArgumentOutOfRangeException(nameof(degreeLevel));

        if (durationInYears is < 1 or > 10)
            throw new ArgumentOutOfRangeException(
                nameof(durationInYears),
                "Program duration must be between 1 and 10 years.");

        DepartmentId = departmentId;
        SetName(name);
        SetCode(code);
        DegreeLevel = degreeLevel;
        DurationInYears = durationInYears;
        IsActive = true;
    }

    public Guid DepartmentId { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public DegreeLevel DegreeLevel { get; private set; }

    public int DurationInYears { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDetails(
        string name,
        string code,
        DegreeLevel degreeLevel,
        int durationInYears)
    {
        if (!Enum.IsDefined(degreeLevel))
            throw new ArgumentOutOfRangeException(nameof(degreeLevel));

        if (durationInYears is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(durationInYears));

        SetName(name);
        SetCode(code);
        DegreeLevel = degreeLevel;
        DurationInYears = durationInYears;
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Program name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Program code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }
}
