using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

internal sealed class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.ShortName).IsUnique();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
    }
}

internal sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("Campuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.HasIndex(x => new { x.UniversityId, x.Code }).IsUnique();
        builder.HasOne<University>().WithMany().HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
