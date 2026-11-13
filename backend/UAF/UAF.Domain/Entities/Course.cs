using UAF.Domain.Common;

namespace UAF.Domain.Entities;

public sealed class Course : AuditableEntity
{
    private Course() { Code = string.Empty; Title = string.Empty; }

    public Course(Guid departmentId, string code, string title, decimal creditHours, string? description = null)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("A department must be specified.", nameof(departmentId));
        DepartmentId = departmentId;
        Code = NormalizeCode(code);
        Title = NormalizeTitle(title);
        CreditHours = NormalizeCreditHours(creditHours);
        Description = NormalizeDescription(description);
        IsActive = true;
    }

    public Guid DepartmentId { get; private set; }
    public string Code { get; private set; }
    public string Title { get; private set; }
    public decimal CreditHours { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(string code, string title, decimal creditHours, string? description)
    {
        var normalizedCode = NormalizeCode(code);
        var normalizedTitle = NormalizeTitle(title);
        var normalizedCreditHours = NormalizeCreditHours(creditHours);
        var normalizedDescription = NormalizeDescription(description);
        Code = normalizedCode;
        Title = normalizedTitle;
        CreditHours = normalizedCreditHours;
        Description = normalizedDescription;
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        MarkUpdated();
    }

    public void Reactivate()
    {
        if (IsActive) return;
        IsActive = true;
        MarkUpdated();
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Course code is required.", nameof(code));
        var value = code.Trim().ToUpperInvariant();
        if (value.Length > 30)
            throw new ArgumentOutOfRangeException(nameof(code), "Course code cannot exceed 30 characters.");
        return value;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Course title is required.", nameof(title));
        var value = title.Trim();
        if (value.Length > 180)
            throw new ArgumentOutOfRangeException(nameof(title), "Course title cannot exceed 180 characters.");
        return value;
    }

    private static decimal NormalizeCreditHours(decimal creditHours)
    {
        if (creditHours <= 0 || creditHours > 10)
            throw new ArgumentOutOfRangeException(nameof(creditHours), "Credit hours must be greater than 0 and no more than 10.");
        return creditHours;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var value = description.Trim();
        if (value.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(description), "Course description cannot exceed 1000 characters.");
        return value;
    }
}