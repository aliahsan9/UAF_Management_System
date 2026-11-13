using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// Links a registered student to a specific course offering.
/// </summary>
public sealed class CourseEnrollment : AuditableEntity
{
    private CourseEnrollment()
    {
    }

    public CourseEnrollment(Guid studentId, Guid courseOfferingId, DateOnly enrolledOn)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("A student must be specified.", nameof(studentId));

        if (courseOfferingId == Guid.Empty)
            throw new ArgumentException("A course offering must be specified.", nameof(courseOfferingId));

        StudentId = studentId;
        CourseOfferingId = courseOfferingId;
        EnrolledOn = enrolledOn;
        Status = EnrollmentStatus.Enrolled;
    }

    public Guid StudentId { get; private set; }

    public Guid CourseOfferingId { get; private set; }

    public DateOnly EnrolledOn { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    public void Drop()
    {
        EnsureEnrolled();
        Status = EnrollmentStatus.Dropped;
        MarkUpdated();
    }

    public void Complete()
    {
        EnsureEnrolled();
        Status = EnrollmentStatus.Completed;
        MarkUpdated();
    }

    public void Withdraw()
    {
        EnsureEnrolled();
        Status = EnrollmentStatus.Withdrawn;
        MarkUpdated();
    }

    private void EnsureEnrolled()
    {
        if (Status != EnrollmentStatus.Enrolled)
            throw new InvalidOperationException("Only an enrolled course registration can change its status.");
    }
}
