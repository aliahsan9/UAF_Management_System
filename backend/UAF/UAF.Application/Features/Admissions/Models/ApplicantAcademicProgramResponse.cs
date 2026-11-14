namespace UAF.Application.Features.Admissions.Models;

public sealed record ApplicantAcademicProgramResponse(
    Guid Id,
    string Name,
    string Code,
    int DegreeLevel,
    int DurationInYears);
