using UAF.Domain.Enums;

namespace UAF.Application.Features.AcademicCatalog.Models;

public sealed record CreateAcademicSessionRequest(string Name, string Code, DateOnly StartDate, DateOnly EndDate);
public sealed record UpdateAcademicSessionRequest(string Name, string Code, DateOnly StartDate, DateOnly EndDate);
public sealed record AcademicSessionResponse(Guid Id, string Name, string Code, DateOnly StartDate, DateOnly EndDate, AcademicSessionStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record CreateCourseRequest(Guid DepartmentId, string Code, string Title, decimal CreditHours, string? Description);
public sealed record UpdateCourseRequest(string Code, string Title, decimal CreditHours, string? Description);
public sealed record CourseResponse(Guid Id, Guid DepartmentId, string Code, string Title, decimal CreditHours, string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);