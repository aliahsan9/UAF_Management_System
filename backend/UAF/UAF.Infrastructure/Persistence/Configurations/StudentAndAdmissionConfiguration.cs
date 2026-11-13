using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.RegistrationNumber).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AdmissionApplicationConfiguration : IEntityTypeConfiguration<AdmissionApplication>
{
    public void Configure(EntityTypeBuilder<AdmissionApplication> builder)
    {
        builder.ToTable("AdmissionApplications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ApplicationNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ApplicantFullName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ApplicantEmail).HasMaxLength(254).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.DecisionRemarks).HasMaxLength(2000);
        builder.HasIndex(x => x.ApplicationNumber).IsUnique();
        builder.HasIndex(x => x.ApplicantEmail);
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
