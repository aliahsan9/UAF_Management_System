using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// A physical or administrative campus belonging to the university.
/// </summary>
public sealed class Campus : AuditableEntity
{
    private Campus()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Campus(Guid universityId, string name, string code, string? address = null)
    {
        if (universityId == Guid.Empty)
            throw new ArgumentException("A university must be specified.", nameof(universityId));

        UniversityId = universityId;
        SetName(name);
        SetCode(code);
        Address = NormalizeOptional(address);
        IsActive = true;
    }

    public Guid UniversityId { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDetails(string name, string code, string? address)
    {
        SetName(name);
        SetCode(code);
        Address = NormalizeOptional(address);
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
            throw new ArgumentException("Campus name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Campus code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
