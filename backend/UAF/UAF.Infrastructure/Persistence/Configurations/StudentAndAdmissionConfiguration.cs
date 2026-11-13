using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
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
        builder.Property(x => x.AdmissionApplicationId);
        builder.Property(x => x.IdentityUserId);
        builder.Property(x => x.RegisteredAtUtc);
        builder.HasIndex(x => x.RegistrationNumber).IsUnique();
        builder.HasIndex(x => x.AdmissionApplicationId).IsUnique().HasFilter("[AdmissionApplicationId] IS NOT NULL");
        builder.HasIndex(x => x.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => new { x.AcademicProgramId, x.Status, x.RegistrationNumber });
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionApplication>().WithMany().HasForeignKey(x => x.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionApplicationConfiguration : IEntityTypeConfiguration<AdmissionApplication>
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
        builder.HasIndex(x => x.DecisionByUserId);
        builder.HasIndex(x => x.ReviewStartedByUserId);
        builder.HasIndex(x => x.ApplicationNumber).IsUnique();
        builder.HasIndex(x => x.ApplicantEmail);
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReviewStartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecisionByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
