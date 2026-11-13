using UAF.API.Authorization;
using UAF.Application.Features.AcademicStructure;
using UAF.Application.Features.AcademicStructure.Models;

namespace UAF.API.Endpoints;

/// <summary>
/// Administrative endpoints for the university's campus and academic hierarchy.
/// All operations require a DepartmentAdmin or SuperAdmin account.
/// </summary>
public static class AcademicStructureEndpoints
{
    public static IEndpointRouteBuilder MapAcademicStructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/academic-structure")
            .WithTags("Academic Structure")
            .RequireAuthorization(AuthorizationPolicies.AcademicStructureAdmin);

        group.MapGet("/universities", GetUniversities)
            .WithName("GetUniversities")
            .Produces<IReadOnlyList<UniversityResponse>>();

        group.MapPost("/universities", CreateUniversity)
            .WithName("CreateUniversity")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/universities/{id:guid}", UpdateUniversity)
            .WithName("UpdateUniversity")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/campuses", GetCampuses)
            .WithName("GetCampuses")
            .Produces<IReadOnlyList<CampusResponse>>();

        group.MapPost("/campuses", CreateCampus)
            .WithName("CreateCampus")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/campuses/{id:guid}", UpdateCampus)
            .WithName("UpdateCampus")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/campuses/{id:guid}", DeactivateCampus)
            .WithName("DeactivateCampus")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/faculties", GetFaculties)
            .WithName("GetFaculties")
            .Produces<IReadOnlyList<FacultyResponse>>();

        group.MapPost("/faculties", CreateFaculty)
            .WithName("CreateFaculty")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/faculties/{id:guid}", UpdateFaculty)
            .WithName("UpdateFaculty")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/faculties/{id:guid}", DeactivateFaculty)
            .WithName("DeactivateFaculty")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/departments", GetDepartments)
            .WithName("GetDepartments")
            .Produces<IReadOnlyList<DepartmentResponse>>();

        group.MapPost("/departments", CreateDepartment)
            .WithName("CreateDepartment")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/departments/{id:guid}", UpdateDepartment)
            .WithName("UpdateDepartment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/departments/{id:guid}", DeactivateDepartment)
            .WithName("DeactivateDepartment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/programs", GetPrograms)
            .WithName("GetAcademicPrograms")
            .Produces<IReadOnlyList<AcademicProgramResponse>>();

        group.MapPost("/programs", CreateProgram)
            .WithName("CreateAcademicProgram")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/programs/{id:guid}", UpdateProgram)
            .WithName("UpdateAcademicProgram")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/programs/{id:guid}", DeactivateProgram)
            .WithName("DeactivateAcademicProgram")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static Task<IReadOnlyList<UniversityResponse>> GetUniversities(
        AcademicStructureService service, CancellationToken ct) => service.GetUniversitiesAsync(ct);

    private static async Task<IResult> CreateUniversity(
        CreateUniversityRequest request, AcademicStructureService service, CancellationToken ct)
    {
        var id = await service.CreateUniversityAsync(request, ct);
        return Results.Created($"/api/v1/academic-structure/universities/{id}", new { id });
    }

    private static async Task<IResult> UpdateUniversity(
        Guid id, UpdateUniversityRequest request, AcademicStructureService service, CancellationToken ct)
    {
        await service.UpdateUniversityAsync(id, request, ct);
        return Results.NoContent();
    }

    private static Task<IReadOnlyList<CampusResponse>> GetCampuses(
        Guid? universityId, AcademicStructureService service, CancellationToken ct) =>
        service.GetCampusesAsync(universityId, ct);

    private static async Task<IResult> CreateCampus(
        CreateCampusRequest request, AcademicStructureService service, CancellationToken ct)
    {
        var id = await service.CreateCampusAsync(request, ct);
        return Results.Created($"/api/v1/academic-structure/campuses/{id}", new { id });
    }

    private static async Task<IResult> UpdateCampus(
        Guid id, UpdateCampusRequest request, AcademicStructureService service, CancellationToken ct)
    {
        await service.UpdateCampusAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateCampus(
        Guid id, AcademicStructureService service, CancellationToken ct)
    {
        await service.DeactivateCampusAsync(id, ct);
        return Results.NoContent();
    }

    private static Task<IReadOnlyList<FacultyResponse>> GetFaculties(
        Guid? universityId, AcademicStructureService service, CancellationToken ct) =>
        service.GetFacultiesAsync(universityId, ct);

    private static async Task<IResult> CreateFaculty(
        CreateFacultyRequest request, AcademicStructureService service, CancellationToken ct)
    {
        var id = await service.CreateFacultyAsync(request, ct);
        return Results.Created($"/api/v1/academic-structure/faculties/{id}", new { id });
    }

    private static async Task<IResult> UpdateFaculty(
        Guid id, UpdateFacultyRequest request, AcademicStructureService service, CancellationToken ct)
    {
        await service.UpdateFacultyAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateFaculty(
        Guid id, AcademicStructureService service, CancellationToken ct)
    {
        await service.DeactivateFacultyAsync(id, ct);
        return Results.NoContent();
    }

    private static Task<IReadOnlyList<DepartmentResponse>> GetDepartments(
        Guid? facultyId, AcademicStructureService service, CancellationToken ct) =>
        service.GetDepartmentsAsync(facultyId, ct);

    private static async Task<IResult> CreateDepartment(
        CreateDepartmentRequest request, AcademicStructureService service, CancellationToken ct)
    {
        var id = await service.CreateDepartmentAsync(request, ct);
        return Results.Created($"/api/v1/academic-structure/departments/{id}", new { id });
    }

    private static async Task<IResult> UpdateDepartment(
        Guid id, UpdateDepartmentRequest request, AcademicStructureService service, CancellationToken ct)
    {
        await service.UpdateDepartmentAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateDepartment(
        Guid id, AcademicStructureService service, CancellationToken ct)
    {
        await service.DeactivateDepartmentAsync(id, ct);
        return Results.NoContent();
    }

    private static Task<IReadOnlyList<AcademicProgramResponse>> GetPrograms(
        Guid? departmentId, AcademicStructureService service, CancellationToken ct) =>
        service.GetProgramsAsync(departmentId, ct);

    private static async Task<IResult> CreateProgram(
        CreateAcademicProgramRequest request, AcademicStructureService service, CancellationToken ct)
    {
        var id = await service.CreateProgramAsync(request, ct);
        return Results.Created($"/api/v1/academic-structure/programs/{id}", new { id });
    }

    private static async Task<IResult> UpdateProgram(
        Guid id, UpdateAcademicProgramRequest request, AcademicStructureService service, CancellationToken ct)
    {
        await service.UpdateProgramAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateProgram(
        Guid id, AcademicStructureService service, CancellationToken ct)
    {
        await service.DeactivateProgramAsync(id, ct);
        return Results.NoContent();
    }
}