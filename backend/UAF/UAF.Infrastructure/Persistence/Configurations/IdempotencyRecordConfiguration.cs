using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.ResponseContentType).HasMaxLength(256);
        builder.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => new { x.ActorKey, x.RequestKey }).IsUnique()
            .HasDatabaseName("UX_IdempotencyRecords_ActorKey_RequestKey");
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
