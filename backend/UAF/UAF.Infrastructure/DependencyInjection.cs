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
using UAF.Application.Features.Authentication.Abstractions;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using UAF.Infrastructure.Persistence.Repositories;

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
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UafDbContext>());
        services.AddScoped<IAdmissionApplicationRepository, AdmissionApplicationRepository>();
        services.AddScoped<IAcademicStructureRepository, AcademicStructureRepository>();
        services.AddScoped<IAcademicCatalogRepository, AcademicCatalogRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
        return services;
    }
}