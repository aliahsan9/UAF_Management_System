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
        string? phoneNumber = null,
        Guid? admissionApplicationId = null,
        Guid? identityUserId = null)
    {
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be specified.", nameof(academicProgramId));

        SetRegistrationNumber(registrationNumber);
        SetFullName(fullName);
        SetEmail(email);

        if (admissionApplicationId == Guid.Empty || identityUserId == Guid.Empty)
            throw new ArgumentException("Optional linked IDs cannot be empty.");
        AcademicProgramId = academicProgramId;
        AdmissionApplicationId = admissionApplicationId;
        IdentityUserId = identityUserId;
        EnrollmentDate = enrollmentDate;
        ValidatePhoneNumber(phoneNumber);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Status = StudentStatus.Active;
    }

    public string RegistrationNumber { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string? PhoneNumber { get; private set; }

    public Guid AcademicProgramId { get; private set; }

    public Guid? AdmissionApplicationId { get; private set; }

    public Guid? IdentityUserId { get; private set; }

    public Guid? RegisteredByUserId { get; private set; }

    public DateTimeOffset? RegisteredAtUtc { get; private set; }

    public void RecordAdmissionRegistration(Guid registrarUserId, DateTimeOffset registeredAtUtc)
    {
        if (registrarUserId == Guid.Empty) throw new ArgumentException("Registrar account is required.", nameof(registrarUserId));
        if (registeredAtUtc == default) throw new ArgumentException("Registration timestamp is required.", nameof(registeredAtUtc));
        if (AdmissionApplicationId is null) throw new InvalidOperationException("Only an admission-converted student can record admission registration.");
        if (RegisteredByUserId.HasValue) throw new InvalidOperationException("Student registration audit has already been recorded.");
        RegisteredByUserId = registrarUserId;
        RegisteredAtUtc = registeredAtUtc.ToUniversalTime();
        MarkUpdated();
    }

    public DateOnly EnrollmentDate { get; private set; }

    public StudentStatus Status { get; private set; }

    public void ChangeAcademicProgram(Guid academicProgramId)
    {
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be specified.", nameof(academicProgramId));
        if (Status is StudentStatus.Graduated or StudentStatus.Withdrawn)
            throw new InvalidOperationException("A graduated or withdrawn student cannot change academic program.");
        AcademicProgramId = academicProgramId;
        MarkUpdated();
    }

    public void UpdateProfile(string fullName, string email, string? phoneNumber)
    {
        ValidateFullName(fullName);
        ValidateEmail(email);
        ValidatePhoneNumber(phoneNumber);

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = NormalizeOptional(phoneNumber);
        MarkUpdated();
    }

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
        if (registrationNumber.Trim().Length > 50)
            throw new ArgumentException("Student registration number cannot exceed 50 characters.", nameof(registrationNumber));

        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
    }

    private void SetFullName(string fullName)
    {
        ValidateFullName(fullName);
        FullName = fullName.Trim();
    }

    private void SetEmail(string email)
    {
        ValidateEmail(email);
        Email = email.Trim().ToLowerInvariant();
    }

    private static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Student full name is required.", nameof(fullName));
        if (fullName.Trim().Length > 180)
            throw new ArgumentException("Student full name cannot exceed 180 characters.", nameof(fullName));
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
            throw new ArgumentException("A valid student email address is required.", nameof(email));
        if (email.Trim().Length > 254)
            throw new ArgumentException("Student email address cannot exceed 254 characters.", nameof(email));
    }

    private static void ValidatePhoneNumber(string? phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Trim().Length > 30)
            throw new ArgumentException("Student phone number cannot exceed 30 characters.", nameof(phoneNumber));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
