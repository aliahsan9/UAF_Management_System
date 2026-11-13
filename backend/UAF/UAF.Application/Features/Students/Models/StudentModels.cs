using UAF.Domain.Enums;

namespace UAF.Application.Features.Students.Models;

public sealed record CreateStudentRequest(
    string RegistrationNumber,
    string FullName,
    string Email,
    Guid AcademicProgramId,
    DateOnly EnrollmentDate,
    string? PhoneNumber);

public sealed record UpdateStudentProfileRequest(string FullName, string Email, string? PhoneNumber);

public sealed record ChangeStudentProgramRequest(Guid AcademicProgramId);

public sealed record StudentResponse(
    Guid Id,
    string RegistrationNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid AcademicProgramId,
    DateOnly EnrollmentDate,
    StudentStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PagedStudentsResponse(
    IReadOnlyList<StudentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);