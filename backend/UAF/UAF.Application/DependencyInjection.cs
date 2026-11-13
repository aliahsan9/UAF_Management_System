using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Admissions;

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

        return services;
    }
}
