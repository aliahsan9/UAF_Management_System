namespace UAF.Domain.Enums;

/// <summary>
/// Availability state of a course offering in an academic session.
/// </summary>
public enum CourseOfferingStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3,
    Cancelled = 4
}
