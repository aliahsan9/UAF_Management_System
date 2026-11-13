using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class RegistrationNumberSeriesConfiguration : IEntityTypeConfiguration<RegistrationNumberSeries>
{
    public void Configure(EntityTypeBuilder<RegistrationNumberSeries> builder)
    {
        builder.ToTable("RegistrationNumberSeries", table =>
            table.HasCheckConstraint("CK_RegistrationNumberSeries_NextNumber", "[NextNumber] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AdmissionYear).IsRequired();
        builder.Property(x => x.NextNumber).IsRequired();
        builder.HasIndex(x => new { x.AcademicProgramId, x.AdmissionYear }).IsUnique();
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfiguredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
