using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.ToTable("CourseEnrollments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.StudentId, x.CourseOfferingId }).IsUnique()
            .HasFilter("[Status] IN (1, 3)"); // Enrolled/completed records are unique; dropped/withdrawn history is retained.
        builder.HasIndex(x => new { x.CourseOfferingId, x.Status });
        builder.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
