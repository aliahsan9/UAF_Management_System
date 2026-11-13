using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// Represents an academic period, such as Fall 2026 or Spring 2027.
/// </summary>
public sealed class AcademicSession : AuditableEntity
{
    private AcademicSession()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public AcademicSession(string name, string code, DateOnly startDate, DateOnly endDate)
    {
        SetName(name);
        SetCode(code);
        ValidateDates(startDate, endDate);

        StartDate = startDate;
        EndDate = endDate;
        Status = AcademicSessionStatus.Planned;
    }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public AcademicSessionStatus Status { get; private set; }

    public void UpdateDetails(string name, string code, DateOnly startDate, DateOnly endDate)
    {
        if (Status is AcademicSessionStatus.Active or AcademicSessionStatus.Completed)
            throw new InvalidOperationException("An active or completed academic session cannot be edited.");

        SetName(name);
        SetCode(code);
        ValidateDates(startDate, endDate);
        StartDate = startDate;
        EndDate = endDate;
        MarkUpdated();
    }

    public void Activate()
    {
        if (Status != AcademicSessionStatus.Planned)
            throw new InvalidOperationException("Only a planned academic session can be activated.");

        Status = AcademicSessionStatus.Active;
        MarkUpdated();
    }

    public void Complete()
    {
        if (Status != AcademicSessionStatus.Active)
            throw new InvalidOperationException("Only an active academic session can be completed.");

        Status = AcademicSessionStatus.Completed;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status is AcademicSessionStatus.Completed or AcademicSessionStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled session cannot be cancelled.");

        Status = AcademicSessionStatus.Cancelled;
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Academic session name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Academic session code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }

    private static void ValidateDates(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("Academic session end date must be after its start date.");
    }
}
