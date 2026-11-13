using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Attendance.Abstractions;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Attendance;
/// <summary>Attendance use cases. The actor ID always comes from the authenticated principal.</summary>
public sealed class AttendanceService
{
    private readonly IAttendanceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public AttendanceService(IAttendanceRepository repository, IUnitOfWork unitOfWork) { _repository = repository; _unitOfWork = unitOfWork; }

    public async Task<Guid> CreateSessionAsync(CreateAttendanceSessionRequest request, Guid actorId, bool privileged, CancellationToken ct, bool departmentAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        if (request.CourseOfferingId == Guid.Empty) throw new ArgumentException("Course offering is required.");
        if (!await _repository.CanManageOfferingAsync(request.CourseOfferingId, actorId, privileged, ct, departmentAdmin))
            throw new UnauthorizedAccessException("You are not assigned to this active course offering.");
        if (await _repository.SessionExistsForDateAsync(request.CourseOfferingId, request.AttendanceDate, ct))
            throw new InvalidOperationException("An attendance session already exists for this offering and date.");
        var session = new AttendanceSession(request.CourseOfferingId, request.AttendanceDate, actorId, request.Topic);
        await _repository.AddSessionAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return session.Id;
    }

    public async Task MarkAsync(Guid sessionId, Guid actorId, bool privileged, MarkAttendanceRequest request, CancellationToken ct, bool departmentAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (sessionId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Session and authenticated actor are required.");
        if (request.Marks is null || request.Marks.Count == 0) throw new ArgumentException("At least one attendance mark is required.");
        if (request.Marks.Count > 500) throw new ArgumentOutOfRangeException(nameof(request), "A single attendance request cannot exceed 500 students.");
        if (request.Marks.Any(x => x.StudentId == Guid.Empty)) throw new ArgumentException("Every mark must identify a student.");
        if (request.Marks.Select(x => x.StudentId).Distinct().Count() != request.Marks.Count)
            throw new ArgumentException("A student can appear only once in a batch.");
        if (request.Marks.Any(x => !Enum.IsDefined(x.Status))) throw new ArgumentOutOfRangeException(nameof(request), "An attendance status is invalid.");
        await _repository.MarkAsync(sessionId, actorId, privileged, request.Marks, ct, departmentAdmin);
    }

    public async Task FinalizeAsync(Guid sessionId, Guid actorId, bool privileged, CancellationToken ct, bool departmentAdmin = false)
    {
        if (sessionId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Session and authenticated actor are required.");
        await _repository.FinalizeAsync(sessionId, actorId, privileged, ct, departmentAdmin);
    }

    public async Task<AttendanceSessionResponse> GetAsync(Guid sessionId, Guid actorId, bool privileged, CancellationToken ct, bool departmentAdmin = false)
    {
        var session = await _repository.GetSessionAsync(sessionId, ct) ?? throw new KeyNotFoundException("Attendance session was not found.");
        if (!await _repository.CanManageOfferingAsync(session.CourseOfferingId, actorId, privileged, ct, departmentAdmin))
            throw new UnauthorizedAccessException("You are not authorized to view attendance for this course offering.");
        var marks = await _repository.GetMarksAsync(sessionId, ct);
        return new(session.Id, session.CourseOfferingId, session.AttendanceDate, session.Topic, session.Status,
            session.CreatedByUserId, session.FinalizedByUserId, session.FinalizedAtUtc,
            marks.Select(x => new AttendanceMarkResponse(x.StudentId, x.Status, x.Remarks, x.MarkedByUserId, x.MarkedAtUtc)).ToArray());
    }
}
