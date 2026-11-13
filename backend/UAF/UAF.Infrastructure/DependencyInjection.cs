using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Infrastructure.Persistence;
using UAF.Infrastructure.Persistence.Repositories;

namespace UAF.Infrastructure;

/// <summary>
/// Registers infrastructure services for dependency injection.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("UafDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'UafDatabase' was not found. Configure it before registering Infrastructure.");

        services.AddDbContext<UafDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<UafDbContext>());

        services.AddScoped<IAdmissionApplicationRepository, AdmissionApplicationRepository>();

        return services;
    }
}
