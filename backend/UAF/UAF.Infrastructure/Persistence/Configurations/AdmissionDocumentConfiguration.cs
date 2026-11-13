using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class AdmissionDocumentConfiguration : IEntityTypeConfiguration<AdmissionDocument>
{
    public void Configure(EntityTypeBuilder<AdmissionDocument> builder)
    {
        builder.ToTable("AdmissionDocuments", table =>
            table.HasCheckConstraint("CK_AdmissionDocuments_SizeBytes", "[SizeBytes] > 0 AND [SizeBytes] <= 10485760"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.VerificationRemarks).HasMaxLength(2000);
        builder.HasIndex(x => new { x.ApplicationId, x.Status });
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasOne<AdmissionApplication>().WithMany().HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
