using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class AdmissionDecisionAuditConfiguration : IEntityTypeConfiguration<AdmissionDecisionAudit>
{
    public void Configure(EntityTypeBuilder<AdmissionDecisionAudit> builder)
    {
        builder.ToTable("AdmissionDecisionAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ApplicationId, x.OccurredAtUtc });
        builder.HasOne<AdmissionApplication>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
