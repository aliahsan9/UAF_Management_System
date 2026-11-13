using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using UAF.API.Authorization;
using UAF.API.Endpoints;
using UAF.API.Extensions;
using UAF.API.Middleware;
using UAF.Application;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdmissionsStaff, policy =>
        policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
    options.AddPolicy(AuthorizationPolicies.AdmissionsDecision, policy =>
        policy.RequireRole("Registrar", "SuperAdmin"));
    options.AddPolicy(AuthorizationPolicies.ApplicantSelfService, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Student"));
    options.AddPolicy(AuthorizationPolicies.AcademicStructureAdmin, policy =>
        policy.RequireRole("DepartmentAdmin", "SuperAdmin"));
    options.AddPolicy(AuthorizationPolicies.StudentManagement, policy =>
        policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
    options.AddPolicy(AuthorizationPolicies.AcademicCatalogManagement, policy =>
        policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
    options.AddPolicy(AuthorizationPolicies.CourseOfferingManagement, policy =>
        policy.RequireRole("Registrar", "DepartmentAdmin", "SuperAdmin"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();
if (app.Configuration.GetValue<bool>("Identity:SeedRolesOnStartup"))
    await IdentityRoleSeeder.SeedRolesAsync(app.Services);

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "UAF Management System API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.MapHealthChecks("/health");
app.MapAdmissionEndpoints();
app.MapApplicantAdmissionEndpoints();
app.MapAcademicStructureEndpoints();
app.MapAcademicCatalogEndpoints();
app.MapStudentEndpoints();
app.MapCourseOfferingEndpoints();
app.MapCourseEnrollmentEndpoints();
app.MapAuthenticationEndpoints();
app.Run();

public partial class Program;
