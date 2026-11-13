using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>
/// Represents a reusable course catalogue entry owned by an academic department.
/// A course is scheduled through one or more CourseOffering records.
/// </summary>
public sealed class Course : AuditableEntity
{
    private Course()
    {
        Code = string.Empty;
        Title = string.Empty;
    }

    public Course(Guid departmentId, string code, string title, decimal creditHours, string? description = null)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("A department must be specified.", nameof(departmentId));

        DepartmentId = departmentId;
        SetCode(code);
        SetTitle(title);
        SetCreditHours(creditHours);
        Description = NormalizeOptional(description);
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
        SetCode(code);
        SetTitle(title);
        SetCreditHours(creditHours);
        Description = NormalizeOptional(description);
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkUpdated();
    }

    public void Reactivate()
    {
        if (IsActive)
            return;

        IsActive = true;
        MarkUpdated();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Course code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Course title is required.", nameof(title));

        Title = title.Trim();
    }

    private void SetCreditHours(decimal creditHours)
    {
        if (creditHours <= 0 || creditHours > 10)
            throw new ArgumentOutOfRangeException(nameof(creditHours), "Credit hours must be greater than 0 and no more than 10.");

        CreditHours = creditHours;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
