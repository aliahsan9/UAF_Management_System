using UAF.Domain.Common;
using UAF.Domain.Enums;

namespace UAF.Domain.Entities;

/// <summary>
/// Represents a scheduled instance of a catalogue course in an academic session.
/// </summary>
public sealed class CourseOffering : AuditableEntity
{
    private CourseOffering()
    {
        Section = string.Empty;
    }

    public CourseOffering(
        Guid courseId,
        Guid academicSessionId,
        string section,
        int capacity,
        Guid? instructorEmployeeId = null)
    {
        if (courseId == Guid.Empty)
            throw new ArgumentException("A course must be specified.", nameof(courseId));

        if (academicSessionId == Guid.Empty)
            throw new ArgumentException("An academic session must be specified.", nameof(academicSessionId));

        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Course section is required.", nameof(section));

        if (section.Trim().Length > 20)
            throw new ArgumentOutOfRangeException(nameof(section), "Course section cannot exceed 20 characters.");

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Course capacity must be greater than zero.");

        if (instructorEmployeeId == Guid.Empty)
            throw new ArgumentException("Instructor employee ID cannot be an empty GUID.", nameof(instructorEmployeeId));

        CourseId = courseId;
        AcademicSessionId = academicSessionId;
        Section = section.Trim().ToUpperInvariant();
        Capacity = capacity;
        InstructorEmployeeId = instructorEmployeeId;
        Status = CourseOfferingStatus.Draft;
    }

    public Guid CourseId { get; private set; }

    public Guid AcademicSessionId { get; private set; }

    public string Section { get; private set; }

    public int Capacity { get; private set; }

    public Guid? InstructorEmployeeId { get; private set; }

    public CourseOfferingStatus Status { get; private set; }

    public void AssignInstructor(Guid? instructorEmployeeId)
    {
        if (Status is CourseOfferingStatus.Closed or CourseOfferingStatus.Cancelled)
            throw new InvalidOperationException("Instructor cannot be changed for a closed or cancelled offering.");

        if (instructorEmployeeId == Guid.Empty)
            throw new ArgumentException("Instructor employee ID cannot be an empty GUID.", nameof(instructorEmployeeId));

        InstructorEmployeeId = instructorEmployeeId;
        MarkUpdated();
    }

    public void UpdateCapacity(int capacity)
    {
        if (Status is CourseOfferingStatus.Closed or CourseOfferingStatus.Cancelled)
            throw new InvalidOperationException("Capacity cannot be changed for a closed or cancelled offering.");

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        Capacity = capacity;
        MarkUpdated();
    }

    public void Publish()
    {
        if (Status != CourseOfferingStatus.Draft)
            throw new InvalidOperationException("Only a draft course offering can be published.");

        Status = CourseOfferingStatus.Published;
        MarkUpdated();
    }

    public void Close()
    {
        if (Status != CourseOfferingStatus.Published)
            throw new InvalidOperationException("Only a published course offering can be closed.");

        Status = CourseOfferingStatus.Closed;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status is CourseOfferingStatus.Closed or CourseOfferingStatus.Cancelled)
            throw new InvalidOperationException("A closed or cancelled offering cannot be cancelled.");

        Status = CourseOfferingStatus.Cancelled;
        MarkUpdated();
    }
}
