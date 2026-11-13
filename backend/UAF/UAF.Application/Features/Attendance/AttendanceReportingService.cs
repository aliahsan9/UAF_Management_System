using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Attendance.Abstractions;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
namespace UAF.Application.Features.Attendance;
public sealed class AttendanceReportingService
{
    private readonly IAttendanceReportingRepository _reporting;
    private readonly IAttendanceRepository _attendance;
    private readonly IUnitOfWork _unitOfWork;
    public AttendanceReportingService(IAttendanceReportingRepository reporting, IAttendanceRepository attendance, IUnitOfWork unitOfWork)
    { _reporting = reporting; _attendance = attendance; _unitOfWork = unitOfWork; }

    public async Task<AttendancePolicyResponse> ConfigurePolicyAsync(Guid academicSessionId, ConfigureAttendancePolicyRequest request, Guid actorId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        if (!await _reporting.AcademicSessionExistsAsync(academicSessionId, ct)) throw new KeyNotFoundException("Academic session was not found.");
        var policy = await _reporting.GetPolicyAsync(academicSessionId, ct);
        if (policy is null)
        {
            policy = new AttendancePolicy(academicSessionId, request.RequiredPercentage, request.LateCreditPercentage, request.ExcludeExcusedFromDenominator, actorId);
            await _reporting.AddPolicyAsync(policy, ct);
        }
        else policy.Update(request.RequiredPercentage, request.LateCreditPercentage, request.ExcludeExcusedFromDenominator, actorId);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToPolicyResponse(policy);
    }

    public async Task<AttendancePolicyResponse> GetPolicyAsync(Guid academicSessionId, CancellationToken ct)
    {
        var policy = await _reporting.GetPolicyAsync(academicSessionId, ct) ?? throw new KeyNotFoundException("Attendance policy was not configured for this academic session.");
        return ToPolicyResponse(policy);
    }

    public async Task<CourseAttendanceReport> GetOfferingReportAsync(Guid offeringId, Guid actorId, bool privileged, CancellationToken ct, bool departmentAdmin = false)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.");
        var source = await _reporting.GetReportSourceAsync(offeringId, ct) ?? throw new KeyNotFoundException("Course offering was not found.");
        if (!await _attendance.CanManageOfferingAsync(offeringId, actorId, privileged, ct, departmentAdmin, allowClosed: true))
            throw new UnauthorizedAccessException("You are not authorized to view this course offering's attendance report.");
        var policy = await _reporting.GetPolicyAsync(source.AcademicSessionId, ct)
            ?? throw new InvalidOperationException("Attendance policy must be configured for this academic session before reports can be calculated.");

        var students = source.Students.Select(student =>
        {
            var present = student.Marks.Count(x => x == UAF.Domain.Enums.AttendanceStatus.Present);
            var absent = student.Marks.Count(x => x == UAF.Domain.Enums.AttendanceStatus.Absent);
            var late = student.Marks.Count(x => x == UAF.Domain.Enums.AttendanceStatus.Late);
            var excused = student.Marks.Count(x => x == UAF.Domain.Enums.AttendanceStatus.Excused);
            var counted = source.FinalizedSessionCount - (policy.ExcludeExcusedFromDenominator ? excused : 0);
            var percentage = AttendancePercentageCalculator.Calculate(source.FinalizedSessionCount, present, late, excused,
                policy.LateCreditPercentage, policy.ExcludeExcusedFromDenominator);
            return new StudentAttendanceReport(student.StudentId, student.RegistrationNumber, student.FullName, student.EnrollmentStatus,
                present, absent, late, excused, counted, percentage,
                percentage.HasValue && percentage.Value < policy.RequiredPercentage);
        }).ToArray();
        var availablePercentages = students.Where(x => x.AttendancePercentage.HasValue).Select(x => x.AttendancePercentage!.Value).ToArray();
        var average = availablePercentages.Length == 0 ? (decimal?)null : Math.Round(availablePercentages.Average(), 2, MidpointRounding.AwayFromZero);
        return new CourseAttendanceReport(source.CourseOfferingId, source.AcademicSessionId, source.FinalizedSessionCount,
            policy.RequiredPercentage, policy.LateCreditPercentage, policy.ExcludeExcusedFromDenominator,
            students.Length, students.Count(x => x.IsBelowRequiredPercentage), average, students);
    }

    private static AttendancePolicyResponse ToPolicyResponse(AttendancePolicy x) =>
        new(x.AcademicSessionId, x.RequiredPercentage, x.LateCreditPercentage, x.ExcludeExcusedFromDenominator, x.ConfiguredByUserId, x.ConfiguredAtUtc);
}
