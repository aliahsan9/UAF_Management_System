using Scalar.AspNetCore;
using UAF.API.Endpoints;
using UAF.API.Middleware;
using UAF.Application;
using UAF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Consistent machine-readable API errors.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// API documentation.
builder.Services.AddOpenApi();

// Application and persistence registrations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Basic liveness endpoint. Database readiness will be added with deployment configuration.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

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

app.Run();

// Exposes the entry point to WebApplicationFactory in integration tests.
public partial class Program;
