using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class AcademicSessionConfiguration : IEntityTypeConfiguration<AcademicSession>
{
    public void Configure(EntityTypeBuilder<AcademicSession> builder)
    {
        builder.ToTable("AcademicSessions", table => table.HasCheckConstraint(
            "CK_AcademicSessions_DateRange", "[EndDate] > [StartDate]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", table => table.HasCheckConstraint(
            "CK_Courses_CreditHours", "[CreditHours] > 0 AND [CreditHours] <= 10"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(180).IsRequired();
        builder.Property(x => x.CreditHours).HasPrecision(4, 2).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}

public sealed class CourseOfferingConfiguration : IEntityTypeConfiguration<CourseOffering>
{
    public void Configure(EntityTypeBuilder<CourseOffering> builder)
    {
        builder.ToTable("CourseOfferings", table => table.HasCheckConstraint(
            "CK_CourseOfferings_Capacity", "[Capacity] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Section).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.CourseId, x.AcademicSessionId, x.Section }).IsUnique();
        builder.HasIndex(x => new { x.AcademicSessionId, x.Status });
        builder.HasIndex(x => x.InstructorEmployeeId);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AcademicSession>().WithMany().HasForeignKey(x => x.AcademicSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.InstructorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
