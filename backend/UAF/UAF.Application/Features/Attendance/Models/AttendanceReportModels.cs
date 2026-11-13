using UAF.Domain.Enums;
namespace UAF.Application.Features.Attendance.Models;
public sealed record ConfigureAttendancePolicyRequest(decimal RequiredPercentage, decimal LateCreditPercentage, bool ExcludeExcusedFromDenominator);
public sealed record AttendancePolicyResponse(Guid AcademicSessionId, decimal RequiredPercentage, decimal LateCreditPercentage, bool ExcludeExcusedFromDenominator, Guid ConfiguredByUserId, DateTimeOffset ConfiguredAtUtc);
public sealed record AttendanceReportStudentSource(Guid StudentId, string RegistrationNumber, string FullName, EnrollmentStatus EnrollmentStatus, IReadOnlyList<AttendanceStatus> Marks);
public sealed record AttendanceReportSource(Guid CourseOfferingId, Guid AcademicSessionId, int FinalizedSessionCount, IReadOnlyList<AttendanceReportStudentSource> Students);
public sealed record StudentAttendanceReport(Guid StudentId, string RegistrationNumber, string FullName, EnrollmentStatus EnrollmentStatus, int PresentCount, int AbsentCount, int LateCount, int ExcusedCount, int CountedSessions, decimal? AttendancePercentage, bool IsBelowRequiredPercentage);
public sealed record CourseAttendanceReport(Guid CourseOfferingId, Guid AcademicSessionId, int FinalizedSessionCount, decimal RequiredPercentage, decimal LateCreditPercentage, bool ExcludeExcusedFromDenominator, int StudentCount, int StudentsBelowThreshold, decimal? AverageAttendancePercentage, IReadOnlyList<StudentAttendanceReport> Students);
