namespace UAF.Domain.Enums;

/// <summary>
/// Lifecycle status of an admission application.
/// </summary>
public enum AdmissionStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Accepted = 4,
    Rejected = 5,
    Waitlisted = 6,
    Withdrawn = 7
}
