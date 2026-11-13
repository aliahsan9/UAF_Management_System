using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence.Configurations;

internal sealed class AdmissionDocumentRequirementConfiguration : IEntityTypeConfiguration<AdmissionDocumentRequirement>
{
    public void Configure(EntityTypeBuilder<AdmissionDocumentRequirement> builder)
    {
        builder.ToTable("AdmissionDocumentRequirements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.HasIndex(x => new { x.AcademicProgramId, x.DocumentType }).IsUnique();
        builder.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.AcademicProgramId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfiguredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
