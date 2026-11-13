using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

public sealed class AcademicSession : AuditableEntity
{
    private AcademicSession() { Name = string.Empty; Code = string.Empty; }

    public AcademicSession(string name, string code, DateOnly startDate, DateOnly endDate)
    {
        Name = NormalizeName(name);
        Code = NormalizeCode(code);
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
        var normalizedName = NormalizeName(name);
        var normalizedCode = NormalizeCode(code);
        ValidateDates(startDate, endDate);
        Name = normalizedName;
        Code = normalizedCode;
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

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Academic session name is required.", nameof(name));
        var value = name.Trim();
        if (value.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(name), "Academic session name cannot exceed 100 characters.");
        return value;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Academic session code is required.", nameof(code));
        var value = code.Trim().ToUpperInvariant();
        if (value.Length > 30)
            throw new ArgumentOutOfRangeException(nameof(code), "Academic session code cannot exceed 30 characters.");
        return value;
    }

    private static void ValidateDates(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("Academic session end date must be after its start date.");
    }
}