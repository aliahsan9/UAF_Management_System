using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Application.Features.AcademicStructure.Abstractions;
using UAF.Application.Features.AcademicCatalog.Abstractions;
using UAF.Application.Features.Students.Abstractions;
using UAF.Application.Features.CourseOfferings.Abstractions;
using UAF.Application.Features.Fees.Abstractions;
using UAF.Application.Features.Employees.Abstractions;
using UAF.Application.Features.Attendance.Abstractions;
using UAF.Application.Features.Examinations.Abstractions;
using UAF.Application.Features.Grading.Abstractions;
using UAF.Application.Features.Transcripts.Abstractions;
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using UAF.Infrastructure.Persistence.Repositories;
using UAF.Infrastructure.Files;
using UAF.Infrastructure.Documents;
using UAF.Infrastructure.Persistence.Interceptors;

namespace UAF.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("UafDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'UafDatabase' was not found. Configure UAF_DB_CONNECTION_STRING or ConnectionStrings__UafDatabase.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) && System.Text.Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "Jwt:SigningKey must contain at least 32 UTF-8 bytes.")
            .Validate(options => options.AccessTokenMinutes is >= 1 and <= 1440, "Jwt:AccessTokenMinutes must be between 1 and 1440.")
            .ValidateOnStart();

        services.AddOptions<AdmissionDocumentStorageOptions>()
            .Bind(configuration.GetSection(AdmissionDocumentStorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "AdmissionDocumentStorage:RootPath is required.")
            .ValidateOnStart();

        services.AddScoped<UafCommandMonitoringInterceptor>();
        services.AddScoped<UafSaveChangesAuditInterceptor>();
        services.AddDbContext<UafDbContext>((serviceProvider, options) =>
            options.UseSqlServer(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<UafCommandMonitoringInterceptor>(),
                    serviceProvider.GetRequiredService<UafSaveChangesAuditInterceptor>()));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<UafDbContext>()
        .AddSignInManager();

        services.AddSingleton<IAdmissionDocumentStorage, LocalAdmissionDocumentStorage>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UafDbContext>());
        services.AddScoped<IAdmissionApplicationRepository, AdmissionApplicationRepository>();
        services.AddScoped<IAdmissionWorkflowRepository, AdmissionWorkflowRepository>();
        services.AddScoped<IAdmissionDocumentRepository, AdmissionDocumentRepository>();
        services.AddScoped<IAcademicStructureRepository, AcademicStructureRepository>();
        services.AddScoped<IAcademicCatalogRepository, AcademicCatalogRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
        services.AddScoped<IFeeRepository, FeeRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAttendanceReportingRepository, AttendanceReportingRepository>();
        services.AddScoped<IExaminationRepository, ExaminationRepository>();
        services.AddScoped<IGradingRepository, GradingRepository>();
        services.AddScoped<IGradeAppealRepository, GradeAppealRepository>();
        services.AddScoped<IGradeRevisionRepository, GradeRevisionRepository>();
        services.AddScoped<ITranscriptRepository, TranscriptRepository>();
        services.AddSingleton<ITranscriptPdfGenerator, QuestPdfTranscriptGenerator>();
        return services;
    }
}