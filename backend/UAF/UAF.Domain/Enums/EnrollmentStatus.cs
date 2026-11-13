namespace UAF.Domain.Enums;

/// <summary>
/// State of a student's registration in a specific course offering.
/// </summary>
public enum EnrollmentStatus
{
    Enrolled = 1,
    Dropped = 2,
    Completed = 3,
    Withdrawn = 4
}
