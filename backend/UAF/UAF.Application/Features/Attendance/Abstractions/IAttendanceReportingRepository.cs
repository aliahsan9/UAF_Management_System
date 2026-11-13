using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Attendance.Abstractions;
public interface IAttendanceReportingRepository
{
    Task<bool> AcademicSessionExistsAsync(Guid academicSessionId, CancellationToken ct);
    Task<AttendancePolicy?> GetPolicyAsync(Guid academicSessionId, CancellationToken ct);
    Task AddPolicyAsync(AttendancePolicy policy, CancellationToken ct);
    Task<AttendanceReportSource?> GetReportSourceAsync(Guid offeringId, CancellationToken ct);
}
