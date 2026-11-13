using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class ExaminationConfiguration : IEntityTypeConfiguration<Examination>
{
    public void Configure(EntityTypeBuilder<Examination> b)
    {
        b.ToTable("Examinations", t =>
        {
            t.HasCheckConstraint("CK_Examinations_Marks", "[MaximumMarks] > 0 AND [PassingMarks] >= 0 AND [PassingMarks] <= [MaximumMarks]");
            t.HasCheckConstraint("CK_Examinations_Weight", "[WeightPercentage] > 0 AND [WeightPercentage] <= 100");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.Type).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ScheduledDate).HasColumnType("date").IsRequired();
        b.Property(x => x.MaximumMarks).HasPrecision(10, 2).IsRequired();
        b.Property(x => x.PassingMarks).HasPrecision(10, 2).IsRequired();
        b.HasIndex(x => new { x.CourseOfferingId, x.Title }).IsUnique();
        b.HasIndex(x => new { x.CourseOfferingId, x.Status, x.ScheduledDate });
        b.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class ExaminationResultConfiguration : IEntityTypeConfiguration<ExaminationResult>
{
    public void Configure(EntityTypeBuilder<ExaminationResult> b)
    {
        b.ToTable("ExaminationResults", t =>
            t.HasCheckConstraint("CK_ExaminationResults_Marks", "[MarksObtained] >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Property(x => x.MarksObtained).HasPrecision(10, 2).IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
        b.Property(x => x.RecordedAtUtc).IsRequired();
        b.HasIndex(x => new { x.ExaminationId, x.StudentId }).IsUnique();
        b.HasOne<Examination>().WithMany().HasForeignKey(x => x.ExaminationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class ExaminationResultAuditConfiguration : IEntityTypeConfiguration<ExaminationResultAudit>
{
    public void Configure(EntityTypeBuilder<ExaminationResultAudit> b)
    {
        b.ToTable("ExaminationResultAudits");
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousMarks).HasPrecision(10, 2);
        b.Property(x => x.NewMarks).HasPrecision(10, 2).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.ChangedAtUtc).IsRequired();
        b.HasIndex(x => new { x.ExaminationId, x.StudentId, x.ChangedAtUtc });
        b.HasOne<Examination>().WithMany().HasForeignKey(x => x.ExaminationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
