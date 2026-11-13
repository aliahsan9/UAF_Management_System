using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.AcademicStructure;
using UAF.Application.Features.Students;

namespace UAF.Application;

/// <summary>
/// Registers application use cases and application-layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AdmissionApplicationService>();
        services.AddScoped<AcademicStructureService>();
        services.AddScoped<StudentService>();

        return services;
    }
}
