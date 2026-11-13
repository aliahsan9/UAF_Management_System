using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class GradeAppealConfiguration : IEntityTypeConfiguration<GradeAppeal>
{
    public void Configure(EntityTypeBuilder<GradeAppeal> b)
    {
        b.ToTable("GradeAppeals", t => t.HasCheckConstraint("CK_GradeAppeals_Status", "[Status] IN (1, 2, 3)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        b.HasIndex(x => new { x.StudentCourseGradeId, x.Status }).IsUnique()
            .HasFilter("[Status] = 1").HasDatabaseName("UX_GradeAppeals_OnePendingPerGrade");
        b.HasIndex(x => new { x.StudentId, x.SubmittedAtUtc });
        b.HasOne<StudentCourseGrade>().WithMany().HasForeignKey(x => x.StudentCourseGradeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class GradeAppealAuditConfiguration : IEntityTypeConfiguration<GradeAppealAudit>
{
    public void Configure(EntityTypeBuilder<GradeAppealAudit> b)
    {
        b.ToTable("GradeAppealAudits", t => t.HasCheckConstraint("CK_GradeAppealAudits_Status", "[PreviousStatus] IN (1, 2, 3) AND [NewStatus] IN (1, 2, 3)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.NewStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
        b.Property(x => x.OccurredAtUtc).IsRequired();
        b.HasIndex(x => new { x.AppealId, x.OccurredAtUtc });
        b.HasOne<GradeAppeal>().WithMany().HasForeignKey(x => x.AppealId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
