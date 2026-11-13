using UAF.Domain.Enums;

namespace UAF.Application.Features.CourseOfferings.Models;

public sealed record CreateCourseOfferingRequest(Guid CourseId, Guid AcademicSessionId, string Section, int Capacity, Guid? InstructorEmployeeId);
public sealed record UpdateCourseOfferingRequest(int Capacity, Guid? InstructorEmployeeId);
public sealed record EnrollStudentRequest(DateOnly EnrolledOn);
public sealed record CourseOfferingResponse(Guid Id, Guid CourseId, Guid AcademicSessionId, string Section, int Capacity, Guid? InstructorEmployeeId, CourseOfferingStatus Status, int EnrolledCount);
public sealed record CourseEnrollmentResponse(Guid Id, Guid StudentId, Guid CourseOfferingId, DateOnly EnrolledOn, EnrollmentStatus Status);