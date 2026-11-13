using UAF.Domain.Entities;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Attendance.Abstractions;
public interface IAttendanceRepository
{
    Task<bool> CanManageOfferingAsync(Guid offeringId, Guid actorUserId, bool privilegedActor, CancellationToken ct, bool departmentAdmin = false, bool allowClosed = false);
    Task<bool> SessionExistsForDateAsync(Guid offeringId, DateOnly date, CancellationToken ct);
    Task AddSessionAsync(AttendanceSession session, CancellationToken ct);
    Task<AttendanceSession?> GetSessionAsync(Guid sessionId, CancellationToken ct);
    Task<IReadOnlyList<StudentAttendance>> GetMarksAsync(Guid sessionId, CancellationToken ct);
    Task MarkAsync(Guid sessionId, Guid actorUserId, bool privilegedActor, IReadOnlyCollection<AttendanceMarkItem> marks, CancellationToken ct, bool departmentAdmin = false);
    Task FinalizeAsync(Guid sessionId, Guid actorUserId, bool privilegedActor, CancellationToken ct, bool departmentAdmin = false);
}
