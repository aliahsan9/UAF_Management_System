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

namespace UAF.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString("UafDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'UafDatabase' was not found. Configure it before registering Infrastructure.");

        services.AddDbContext<UafDbContext>(options => options.UseSqlServer(connectionString));
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

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdmissionDocumentStorageOptions>(configuration.GetSection(AdmissionDocumentStorageOptions.SectionName));
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
