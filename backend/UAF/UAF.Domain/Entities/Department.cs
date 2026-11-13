using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// An academic department that belongs to a faculty.
/// </summary>
public sealed class Department : AuditableEntity
{
    private Department()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Department(Guid facultyId, string name, string code)
    {
        if (facultyId == Guid.Empty)
            throw new ArgumentException("A faculty must be specified.", nameof(facultyId));

        FacultyId = facultyId;
        SetName(name);
        SetCode(code);
        IsActive = true;
    }

    public Guid FacultyId { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDetails(string name, string code)
    {
        SetName(name);
        SetCode(code);
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
            throw new ArgumentException("Department name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Department code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }
}
