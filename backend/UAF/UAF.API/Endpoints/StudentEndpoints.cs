using UAF.API.Authorization;
using UAF.Application.Features.Students;
using UAF.Application.Features.Students.Models;
using UAF.Domain.Enums;

namespace UAF.API.Endpoints;

/// <summary>
/// Administrative student records. Student self-service is intentionally separate
/// until a trusted Identity-user-to-student relationship is implemented.
/// </summary>
public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/students")
            .WithTags("Students")
            .RequireAuthorization(AuthorizationPolicies.StudentManagement);

        group.MapGet("/", GetStudents)
            .WithName("GetStudents")
            .Produces<PagedStudentsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetStudent)
            .WithName("GetStudent")
            .Produces<StudentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateStudent)
            .WithName("CreateStudent")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/profile", UpdateStudentProfile)
            .WithName("UpdateStudentProfile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/academic-program", ChangeStudentProgram)
            .WithName("ChangeStudentAcademicProgram")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/suspend", SuspendStudent)
            .WithName("SuspendStudent")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reactivate", ReactivateStudent)
            .WithName("ReactivateStudent")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/graduate", GraduateStudent)
            .WithName("GraduateStudent")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/withdraw", WithdrawStudent)
            .WithName("WithdrawStudent")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static Task<PagedStudentsResponse> GetStudents(
        int page,
        int pageSize,
        Guid? academicProgramId,
        StudentStatus? status,
        string? search,
        string sortBy,
        string sortDirection,
        StudentService service,
        CancellationToken ct) =>
        service.GetPageAsync(page, pageSize, academicProgramId, status, search, sortBy, sortDirection, ct);

    private static async Task<IResult> GetStudent(Guid id, StudentService service, CancellationToken ct) =>
        Results.Ok(await service.GetByIdAsync(id, ct));

    private static async Task<IResult> CreateStudent(
        CreateStudentRequest request, StudentService service, CancellationToken ct)
    {
        var id = await service.CreateAsync(request, ct);
        return Results.Created($"/api/v1/students/{id}", new { id });
    }

    private static async Task<IResult> UpdateStudentProfile(
        Guid id, UpdateStudentProfileRequest request, StudentService service, CancellationToken ct)
    {
        await service.UpdateProfileAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangeStudentProgram(
        Guid id, ChangeStudentProgramRequest request, StudentService service, CancellationToken ct)
    {
        await service.ChangeProgramAsync(id, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SuspendStudent(Guid id, StudentService service, CancellationToken ct)
    {
        await service.SuspendAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReactivateStudent(Guid id, StudentService service, CancellationToken ct)
    {
        await service.ReactivateAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GraduateStudent(Guid id, StudentService service, CancellationToken ct)
    {
        await service.GraduateAsync(id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> WithdrawStudent(Guid id, StudentService service, CancellationToken ct)
    {
        await service.WithdrawAsync(id, ct);
        return Results.NoContent();
    }
}