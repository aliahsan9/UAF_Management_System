using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class GradeRevisionRequestConfiguration : IEntityTypeConfiguration<GradeRevisionRequest>
{
    public void Configure(EntityTypeBuilder<GradeRevisionRequest> b)
    {
        b.ToTable("GradeRevisionRequests", t =>
        {
            t.HasCheckConstraint("CK_GradeRevisionRequests_Status", "[Status] IN (1, 2, 3)");
            t.HasCheckConstraint("CK_GradeRevisionRequests_ProposedMarks", "[ProposedMarksObtained] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.ReviewRemarks).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ProposedMarksObtained).HasPrecision(10, 2).IsRequired();
        b.HasIndex(x => new { x.StudentCourseGradeId, x.Status }).IsUnique()
            .HasFilter("[Status] = 1").HasDatabaseName("UX_GradeRevisionRequests_OnePendingPerGrade");
        b.HasIndex(x => x.AppealId).IsUnique();
        b.HasOne<StudentCourseGrade>().WithMany().HasForeignKey(x => x.StudentCourseGradeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<GradeAppeal>().WithMany().HasForeignKey(x => x.AppealId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class GradeRevisionConfiguration : IEntityTypeConfiguration<GradeRevision>
{
    public void Configure(EntityTypeBuilder<GradeRevision> b)
    {
        b.ToTable("GradeRevisions", t =>
        {
            t.HasCheckConstraint("CK_GradeRevisions_RevisionNumber", "[RevisionNumber] > 0");
            t.HasCheckConstraint("CK_GradeRevisions_Marks", "[PreviousMarks] >= 0 AND [NewMarks] >= 0");
            t.HasCheckConstraint("CK_GradeRevisions_Percentage", "[PreviousPercentage] BETWEEN 0 AND 100 AND [NewPercentage] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_GradeRevisions_GradePoints", "[PreviousGradePoints] BETWEEN 0 AND 10 AND [NewGradePoints] BETWEEN 0 AND 10");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousLetterGrade).HasMaxLength(10).IsRequired();
        b.Property(x => x.NewLetterGrade).HasMaxLength(10).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PreviousMarks).HasPrecision(10, 2);
        b.Property(x => x.NewMarks).HasPrecision(10, 2);
        b.Property(x => x.PreviousPercentage).HasPrecision(5, 2);
        b.Property(x => x.NewPercentage).HasPrecision(5, 2);
        b.Property(x => x.PreviousGradePoints).HasPrecision(4, 2);
        b.Property(x => x.NewGradePoints).HasPrecision(4, 2);
        b.HasIndex(x => new { x.StudentCourseGradeId, x.RevisionNumber }).IsUnique();
        b.HasIndex(x => x.RequestId).IsUnique();
        b.HasOne<GradeRevisionRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StudentCourseGrade>().WithMany().HasForeignKey(x => x.StudentCourseGradeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class GradeRevisionAuditConfiguration : IEntityTypeConfiguration<GradeRevisionAudit>
{
    public void Configure(EntityTypeBuilder<GradeRevisionAudit> b)
    {
        b.ToTable("GradeRevisionAudits", t =>
        {
            t.HasCheckConstraint("CK_GradeRevisionAudits_Status", "[PreviousStatus] = 1 AND [NewStatus] IN (2, 3)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.NewStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => x.RequestId).IsUnique();
        b.HasIndex(x => new { x.RequestId, x.OccurredAtUtc });
        b.HasOne<GradeRevisionRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
