using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// An academic faculty within the university.
/// </summary>
public sealed class Faculty : AuditableEntity
{
    private Faculty()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Faculty(Guid universityId, string name, string code)
    {
        if (universityId == Guid.Empty)
            throw new ArgumentException("A university must be specified.", nameof(universityId));

        UniversityId = universityId;
        SetName(name);
        SetCode(code);
        IsActive = true;
    }

    public Guid UniversityId { get; private set; }

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
            throw new ArgumentException("Faculty name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Faculty code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }
}
