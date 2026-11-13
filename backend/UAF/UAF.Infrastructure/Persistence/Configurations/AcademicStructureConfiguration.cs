using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("Faculties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.UniversityId, x.Code }).IsUnique();
        builder.HasOne<University>().WithMany().HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.FacultyId, x.Code }).IsUnique();
        builder.HasOne<Faculty>().WithMany().HasForeignKey(x => x.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("AcademicPrograms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DegreeLevel).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
