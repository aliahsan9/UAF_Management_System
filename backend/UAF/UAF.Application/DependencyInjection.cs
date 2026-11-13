using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Common.Caching;
using UAF.Application.Features.Admissions;
using UAF.Application.Features.AcademicStructure;
using UAF.Application.Features.AcademicCatalog;
using UAF.Application.Features.Students;
using UAF.Application.Features.CourseOfferings;
using UAF.Application.Features.Fees;
using UAF.Application.Features.Employees;
using UAF.Application.Features.Attendance;
using UAF.Application.Features.Examinations;
using UAF.Application.Features.Grading;
using UAF.Application.Features.Transcripts;

namespace UAF.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMemoryCache();
        services.AddHybridCache();
        services.AddScoped<IApplicationCache, HybridApplicationCache>();
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
        services.AddScoped<ExaminationService>();
        services.AddScoped<GradingService>();
        services.AddScoped<GradeAppealService>();
        services.AddScoped<GradeRevisionService>();
        services.AddScoped<TranscriptService>();
        return services;
    }
}
