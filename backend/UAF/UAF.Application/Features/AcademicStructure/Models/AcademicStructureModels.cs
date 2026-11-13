using UAF.Domain.Enums;

namespace UAF.Application.Features.AcademicStructure.Models;

public sealed record CreateUniversityRequest(string Name, string ShortName);
public sealed record UpdateUniversityRequest(string Name, string ShortName);
public sealed record UniversityResponse(Guid Id, string Name, string ShortName);

public sealed record CreateCampusRequest(Guid UniversityId, string Name, string Code, string? Address);
public sealed record UpdateCampusRequest(string Name, string Code, string? Address);
public sealed record CampusResponse(Guid Id, Guid UniversityId, string Name, string Code, string? Address, bool IsActive);

public sealed record CreateFacultyRequest(Guid UniversityId, string Name, string Code);
public sealed record UpdateFacultyRequest(string Name, string Code);
public sealed record FacultyResponse(Guid Id, Guid UniversityId, string Name, string Code, bool IsActive);

public sealed record CreateDepartmentRequest(Guid FacultyId, string Name, string Code);
public sealed record UpdateDepartmentRequest(string Name, string Code);
public sealed record DepartmentResponse(Guid Id, Guid FacultyId, string Name, string Code, bool IsActive);

public sealed record CreateAcademicProgramRequest(
    Guid DepartmentId,
    string Name,
    string Code,
    DegreeLevel DegreeLevel,
    int DurationInYears);

public sealed record UpdateAcademicProgramRequest(
    string Name,
    string Code,
    DegreeLevel DegreeLevel,
    int DurationInYears);

public sealed record AcademicProgramResponse(
    Guid Id,
    Guid DepartmentId,
    string Name,
    string Code,
    DegreeLevel DegreeLevel,
    int DurationInYears,
    bool IsActive);