using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// Represents a student who has been formally registered with the university.
/// Applicant information belongs to AdmissionApplication until admission is accepted.
/// </summary>
public sealed class Student : AuditableEntity
{
    private Student()
    {
        RegistrationNumber = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
    }

    public Student(
        string registrationNumber,
        string fullName,
        string email,
        Guid academicProgramId,
        DateOnly enrollmentDate,
        string? phoneNumber = null)
    {
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be specified.", nameof(academicProgramId));

        SetRegistrationNumber(registrationNumber);
        SetFullName(fullName);
        SetEmail(email);

        AcademicProgramId = academicProgramId;
        EnrollmentDate = enrollmentDate;
        PhoneNumber = NormalizeOptional(phoneNumber);
        Status = StudentStatus.Active;
    }

    public string RegistrationNumber { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string? PhoneNumber { get; private set; }

    public Guid AcademicProgramId { get; private set; }

    public DateOnly EnrollmentDate { get; private set; }

    public StudentStatus Status { get; private set; }

    public void UpdateContactDetails(string email, string? phoneNumber)
    {
        SetEmail(email);
        PhoneNumber = NormalizeOptional(phoneNumber);
        MarkUpdated();
    }

    public void UpdateFullName(string fullName)
    {
        SetFullName(fullName);
        MarkUpdated();
    }

    public void Suspend()
    {
        EnsureStatusIs(StudentStatus.Active);

        Status = StudentStatus.Suspended;
        MarkUpdated();
    }

    public void Reactivate()
    {
        EnsureStatusIs(StudentStatus.Suspended);

        Status = StudentStatus.Active;
        MarkUpdated();
    }

    public void MarkAsGraduated()
    {
        if (Status is not StudentStatus.Active and not StudentStatus.Suspended)
            throw new InvalidOperationException("Only an active or suspended student can be marked as graduated.");

        Status = StudentStatus.Graduated;
        MarkUpdated();
    }

    public void Withdraw()
    {
        if (Status is StudentStatus.Graduated or StudentStatus.Withdrawn)
            throw new InvalidOperationException("A graduated or withdrawn student cannot be withdrawn again.");

        Status = StudentStatus.Withdrawn;
        MarkUpdated();
    }

    private void EnsureStatusIs(StudentStatus expectedStatus)
    {
        if (Status != expectedStatus)
            throw new InvalidOperationException($"This operation requires student status {expectedStatus}.");
    }

    private void SetRegistrationNumber(string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Student registration number is required.", nameof(registrationNumber));

        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
    }

    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Student full name is required.", nameof(fullName));

        FullName = fullName.Trim();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
            throw new ArgumentException("A valid student email address is required.", nameof(email));

        Email = email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
