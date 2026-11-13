using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// Represents the institution managed by this system.
/// The initial deployment is modelled for one university.
/// </summary>
public sealed class University : AuditableEntity
{
    private University()
    {
        Name = string.Empty;
        ShortName = string.Empty;
    }

    public University(string name, string shortName)
    {
        SetName(name);
        SetShortName(shortName);
    }

    public string Name { get; private set; }

    public string ShortName { get; private set; }

    public void UpdateDetails(string name, string shortName)
    {
        SetName(name);
        SetShortName(shortName);
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("University name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetShortName(string shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException("University short name is required.", nameof(shortName));

        ShortName = shortName.Trim().ToUpperInvariant();
    }
}
