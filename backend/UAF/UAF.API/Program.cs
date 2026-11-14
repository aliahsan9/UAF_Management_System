using System.IO.Compression;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using UAF.API.Authorization;
using UAF.API.Endpoints;
using UAF.API.Extensions;
using UAF.API.Health;
using UAF.API.Middleware;
using UAF.API.Validation;
using UAF.Application;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var minimumLevel = context.HostingEnvironment.IsDevelopment()
        ? LogEventLevel.Debug
        : LogEventLevel.Information;

    loggerConfiguration
        .MinimumLevel.Is(minimumLevel)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "UAF.ManagementSystem")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File("logs/uaf-.log", rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14, shared: true);
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);

// Limit multipart buffering and total request size for document-upload endpoints.
const long maximumUploadRequestBytes = 11 * 1024 * 1024;
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maximumUploadRequestBytes;
    options.ValueLengthLimit = 16 * 1024;
    options.MultipartHeadersLengthLimit = 16 * 1024;
    options.MemoryBufferThreshold = 64 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = maximumUploadRequestBytes);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("UafFrontend", policy =>
{
    if (allowedOrigins.Length == 0)
        policy.SetIsOriginAllowed(_ => false);
    else
        policy.WithOrigins(allowedOrigins);

    policy.AllowAnyMethod()
        .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID", "X-Refresh-Request", "Idempotency-Key")
        .WithExposedHeaders("X-Correlation-ID", "Idempotency-Replayed")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
}));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAdmissionDraftRequestValidator>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddUafAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("transcript-verification", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready", "database"]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Identity:SeedRolesOnStartup"))
    await IdentityRoleSeeder.SeedRolesAsync(app.Services);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
    };
});
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseResponseCompression();
app.UseMiddleware<ReferenceDataCacheInvalidationMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseCors("UafFrontend");
app.UseAuthentication();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<SecurityAuditMiddleware>();
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

static Task WriteHealthResponse(HttpContext context, HealthReport report) =>
    context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            durationMs = entry.Value.Duration.TotalMilliseconds
        })
    }, cancellationToken: context.RequestAborted);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

app.MapAdmissionEndpoints();
app.MapApplicantAdmissionEndpoints();
app.MapAdmissionDocumentReviewEndpoints();
app.MapAcademicStructureEndpoints();
app.MapAcademicCatalogEndpoints();
app.MapStudentEndpoints();
app.MapCourseOfferingEndpoints();
app.MapCourseEnrollmentEndpoints();
app.MapFeeEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();
app.MapExaminationEndpoints();
app.MapGradingEndpoints();
app.MapGradeAppealEndpoints();
app.MapGradeRevisionEndpoints();
app.MapTranscriptEndpoints();
app.MapTimetableEndpoints();
app.MapAuthenticationEndpoints();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
