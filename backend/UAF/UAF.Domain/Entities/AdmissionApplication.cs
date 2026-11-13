using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// An applicant's request for admission. OwnerUserId is assigned by the server
/// from the authenticated Identity principal and is never accepted from a client.
/// Null is retained only for legacy/staff-created records pending controlled linking.
/// </summary>
public sealed class AdmissionApplication : AuditableEntity
{
    private AdmissionApplication()
    {
        ApplicationNumber = string.Empty;
        ApplicantFullName = string.Empty;
        ApplicantEmail = string.Empty;
    }

    public AdmissionApplication(
        string applicationNumber,
        string applicantFullName,
        string applicantEmail,
        Guid academicProgramId,
        DateOnly applicationDate,
        string? phoneNumber = null,
        Guid? ownerUserId = null)
    {
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("An academic program must be specified.", nameof(academicProgramId));
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Applicant account ID cannot be empty.", nameof(ownerUserId));

        SetApplicationNumber(applicationNumber);
        SetApplicantFullName(applicantFullName);
        SetApplicantEmail(applicantEmail);
        AcademicProgramId = academicProgramId;
        ApplicationDate = applicationDate;
        PhoneNumber = NormalizeOptional(phoneNumber);
        OwnerUserId = ownerUserId;
        Status = AdmissionStatus.Draft;
    }

    public string ApplicationNumber { get; private set; }
    public string ApplicantFullName { get; private set; }
    public string ApplicantEmail { get; private set; }
    public string? PhoneNumber { get; private set; }
    public Guid AcademicProgramId { get; private set; }
    public DateOnly ApplicationDate { get; private set; }
    public AdmissionStatus Status { get; private set; }
    public string? DecisionRemarks { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public Guid? ReviewStartedByUserId { get; private set; }
    public DateTimeOffset? ReviewStartedAtUtc { get; private set; }
    public Guid? DecisionByUserId { get; private set; }
    public DateTimeOffset? DecisionAtUtc { get; private set; }

    public void UpdateApplicantDetails(string fullName, string email, string? phoneNumber)
    {
        EnsureEditable();
        SetApplicantFullName(fullName);
        SetApplicantEmail(email);
        PhoneNumber = NormalizeOptional(phoneNumber);
        MarkUpdated();
    }

    public void Submit()
    {
        if (Status != AdmissionStatus.Draft)
            throw new InvalidOperationException("Only a draft application can be submitted.");
        Status = AdmissionStatus.Submitted;
        MarkUpdated();
    }

    public void StartReview(Guid reviewerUserId, DateTimeOffset occurredAtUtc)
    {
        EnsureActorAndTimestamp(reviewerUserId, occurredAtUtc);
        if (Status != AdmissionStatus.Submitted)
            throw new InvalidOperationException("Only a submitted application can enter review.");
        Status = AdmissionStatus.UnderReview;
        ReviewStartedByUserId = reviewerUserId;
        ReviewStartedAtUtc = occurredAtUtc.ToUniversalTime();
        MarkUpdated();
    }

    public void Accept(Guid actorUserId, DateTimeOffset occurredAtUtc, string? remarks = null) =>
        RecordDecision(AdmissionStatus.Accepted, actorUserId, occurredAtUtc, remarks);

    public void Reject(Guid actorUserId, DateTimeOffset occurredAtUtc, string? remarks = null) =>
        RecordDecision(AdmissionStatus.Rejected, actorUserId, occurredAtUtc, remarks);

    public void Waitlist(Guid actorUserId, DateTimeOffset occurredAtUtc, string? remarks = null) =>
        RecordDecision(AdmissionStatus.Waitlisted, actorUserId, occurredAtUtc, remarks);

    private void RecordDecision(AdmissionStatus decision, Guid actorUserId, DateTimeOffset occurredAtUtc, string? remarks)
    {
        EnsureActorAndTimestamp(actorUserId, occurredAtUtc);
        EnsureUnderReview();
        var normalizedRemarks = NormalizeOptional(remarks);
        if (normalizedRemarks is null)
            throw new ArgumentException("Decision remarks are required.", nameof(remarks));
        if (normalizedRemarks?.Length > 2000)
            throw new ArgumentException("Decision remarks cannot exceed 2000 characters.", nameof(remarks));
        Status = decision;
        DecisionRemarks = normalizedRemarks;
        DecisionByUserId = actorUserId;
        DecisionAtUtc = occurredAtUtc.ToUniversalTime();
        MarkUpdated();
    }

    private static void EnsureActorAndTimestamp(Guid actorUserId, DateTimeOffset occurredAtUtc)
    {
        if (actorUserId == Guid.Empty) throw new ArgumentException("An authenticated staff actor is required.", nameof(actorUserId));
        if (occurredAtUtc == default) throw new ArgumentException("A UTC timestamp is required.", nameof(occurredAtUtc));
    }

    public void Withdraw()
    {
        if (Status is AdmissionStatus.Accepted or AdmissionStatus.Rejected or AdmissionStatus.Withdrawn)
            throw new InvalidOperationException("This application can no longer be withdrawn.");
        Status = AdmissionStatus.Withdrawn;
        MarkUpdated();
    }

    private void EnsureEditable()
    {
        if (Status != AdmissionStatus.Draft)
            throw new InvalidOperationException("Applicant details can only be changed while the application is a draft.");
    }

    private void EnsureUnderReview()
    {
        if (Status != AdmissionStatus.UnderReview)
            throw new InvalidOperationException("A decision can only be recorded for an application under review.");
    }

    private void SetApplicationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 50)
            throw new ArgumentException("Application number is required and cannot exceed 50 characters.", nameof(value));
        ApplicationNumber = value.Trim().ToUpperInvariant();
    }

    private void SetApplicantFullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 180)
            throw new ArgumentException("Applicant name is required and cannot exceed 180 characters.", nameof(value));
        ApplicantFullName = value.Trim();
    }

    private void SetApplicantEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 254 ||
            !System.Net.Mail.MailAddress.TryCreate(value.Trim(), out var address) ||
            !string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A valid applicant email address is required.", nameof(value));
        ApplicantEmail = value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
