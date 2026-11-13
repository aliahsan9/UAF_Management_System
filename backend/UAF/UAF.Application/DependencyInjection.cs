using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.AcademicStructure;
using UAF.Application.Features.AcademicCatalog;
using UAF.Application.Features.Students;
using UAF.Application.Features.CourseOfferings;
using UAF.Application.Features.Fees;
using UAF.Application.Features.Employees;
using UAF.Application.Features.Attendance;
using UAF.Application.Features.Examinations;

namespace UAF.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AdmissionApplicationService>();
        services.AddScoped<AdmissionWorkflowService>();
        services.AddScoped<AdmissionDocumentService>();
        services.AddScoped<AcademicStructureService>();
        services.AddScoped<AcademicCatalogService>();
        services.AddScoped<StudentService>();
        services.AddScoped<CourseOfferingService>();
        services.AddScoped<FeeService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<AttendanceReportingService>();
        return services;
    }
}
