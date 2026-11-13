using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.ToTable("AttendanceSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.AttendanceDate).HasColumnType("date").IsRequired();
        b.Property(x => x.Topic).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.HasIndex(x => new { x.CourseOfferingId, x.AttendanceDate }).IsUnique();
        b.HasIndex(x => new { x.CourseOfferingId, x.Status, x.AttendanceDate });
        b.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class StudentAttendanceConfiguration : IEntityTypeConfiguration<StudentAttendance>
{
    public void Configure(EntityTypeBuilder<StudentAttendance> b)
    {
        b.ToTable("StudentAttendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
        b.Property(x => x.MarkedAtUtc).IsRequired();
        b.HasIndex(x => new { x.AttendanceSessionId, x.StudentId }).IsUnique();
        b.HasIndex(x => new { x.StudentId, x.Status });
        b.HasOne<AttendanceSession>().WithMany().HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.MarkedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class AttendanceChangeAuditConfiguration : IEntityTypeConfiguration<AttendanceChangeAudit>
{
    public void Configure(EntityTypeBuilder<AttendanceChangeAudit> b)
    {
        b.ToTable("AttendanceChangeAudits");
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousStatus).HasConversion<int?>();
        b.Property(x => x.NewStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.ChangedAtUtc).IsRequired();
        b.HasIndex(x => new { x.AttendanceSessionId, x.StudentId, x.ChangedAtUtc });
        b.HasOne<AttendanceSession>().WithMany().HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
