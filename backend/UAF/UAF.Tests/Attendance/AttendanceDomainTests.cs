using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Attendance;
public sealed class AttendanceDomainTests
{
    [Fact]
    public void Session_can_be_finalized_once_and_records_actor()
    {
        var session = new AttendanceSession(Guid.NewGuid(), new DateOnly(2026, 11, 13), Guid.NewGuid(), "Lab 01");
        var actor = Guid.NewGuid();
        session.Finalize(actor, DateTimeOffset.UtcNow);
        Assert.Equal(AttendanceSessionStatus.Finalized, session.Status);
        Assert.Equal(actor, session.FinalizedByUserId);
        Assert.Throws<InvalidOperationException>(() => session.Finalize(actor, DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Attendance_mark_correction_requires_a_reason_and_updates_actor()
    {
        var mark = new StudentAttendance(Guid.NewGuid(), Guid.NewGuid(), AttendanceStatus.Absent, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => mark.Correct(AttendanceStatus.Present, Guid.NewGuid(), DateTimeOffset.UtcNow, ""));
        var actor = Guid.NewGuid();
        mark.Correct(AttendanceStatus.Present, actor, DateTimeOffset.UtcNow, "Verified supporting record");
        Assert.Equal(AttendanceStatus.Present, mark.Status);
        Assert.Equal(actor, mark.MarkedByUserId);
    }
    [Fact]
    public void Audit_requires_actor_and_valid_status()
    {
        Assert.Throws<ArgumentException>(() => new AttendanceChangeAudit(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, AttendanceStatus.Present, null, DateTimeOffset.UtcNow));
        var audit = new AttendanceChangeAudit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, AttendanceStatus.Present, null, DateTimeOffset.UtcNow);
        Assert.Null(audit.PreviousStatus);
    }
}
