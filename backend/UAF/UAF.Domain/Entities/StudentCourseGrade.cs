using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Finalized immutable course-grade snapshot used for academic records and GPA.</summary>
public sealed class StudentCourseGrade : AuditableEntity
{
    private StudentCourseGrade() { CourseCode = string.Empty; CourseTitle = string.Empty; LetterGrade = string.Empty; }
    public StudentCourseGrade(Guid studentId, Guid courseOfferingId, Guid courseId, Guid academicSessionId, Guid gradeScaleId,
        string courseCode, string courseTitle, decimal creditHours, decimal totalMarks, decimal marksObtained,
        decimal percentage, string letterGrade, decimal gradePoints, bool isPassed, bool includeFailedCoursesInGpa,
        RepeatCoursePolicy repeatCoursePolicy, Guid finalizedByUserId, DateTimeOffset finalizedAtUtc)
    {
        if (studentId == Guid.Empty || courseOfferingId == Guid.Empty || courseId == Guid.Empty || academicSessionId == Guid.Empty || gradeScaleId == Guid.Empty || finalizedByUserId == Guid.Empty)
            throw new ArgumentException("Student, course, offering, session, scale and finalizing user are required.");
        if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(courseTitle)) throw new ArgumentException("Course snapshot details are required.");
        if (creditHours <= 0 || creditHours > 10) throw new ArgumentOutOfRangeException(nameof(creditHours));
        if (totalMarks <= 0 || marksObtained < 0 || marksObtained > totalMarks) throw new ArgumentOutOfRangeException(nameof(marksObtained));
        if (percentage < 0 || percentage > 100 || gradePoints < 0 || gradePoints > 10) throw new ArgumentOutOfRangeException(nameof(percentage));
        if (string.IsNullOrWhiteSpace(letterGrade) || letterGrade.Trim().Length > 10) throw new ArgumentException("Letter grade is required.");
        if (!Enum.IsDefined(repeatCoursePolicy)) throw new ArgumentOutOfRangeException(nameof(repeatCoursePolicy));
        if (finalizedAtUtc == default) throw new ArgumentException("Finalization timestamp is required.");
        StudentId = studentId; CourseOfferingId = courseOfferingId; CourseId = courseId; AcademicSessionId = academicSessionId; GradeScaleId = gradeScaleId;
        CourseCode = courseCode.Trim(); CourseTitle = courseTitle.Trim(); CreditHours = creditHours; TotalMarks = totalMarks; MarksObtained = marksObtained;
        Percentage = percentage; LetterGrade = letterGrade.Trim().ToUpperInvariant(); GradePoints = gradePoints; IsPassed = isPassed;
        IncludeFailedCoursesInGpa = includeFailedCoursesInGpa; RepeatCoursePolicy = repeatCoursePolicy;
        FinalizedByUserId = finalizedByUserId; FinalizedAtUtc = finalizedAtUtc.ToUniversalTime();
    }
    public Guid StudentId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid AcademicSessionId { get; private set; }
    public Guid GradeScaleId { get; private set; }
    public string CourseCode { get; private set; }
    public string CourseTitle { get; private set; }
    public decimal CreditHours { get; private set; }
    public decimal TotalMarks { get; private set; }
    public decimal MarksObtained { get; private set; }
    public decimal Percentage { get; private set; }
    public string LetterGrade { get; private set; }
    public decimal GradePoints { get; private set; }
    public bool IsPassed { get; private set; }
    public bool IncludeFailedCoursesInGpa { get; private set; }
    public RepeatCoursePolicy RepeatCoursePolicy { get; private set; }
    public Guid FinalizedByUserId { get; private set; }
    public DateTimeOffset FinalizedAtUtc { get; private set; }
}
