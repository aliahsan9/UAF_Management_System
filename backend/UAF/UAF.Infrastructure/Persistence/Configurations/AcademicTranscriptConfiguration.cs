using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class AcademicTranscriptConfiguration : IEntityTypeConfiguration<AcademicTranscript>
{
    public void Configure(EntityTypeBuilder<AcademicTranscript> b)
    {
        b.ToTable("AcademicTranscripts");
        b.HasKey(x => x.Id);
        b.Property(x => x.VerificationCode).HasMaxLength(32).IsUnicode(false).IsRequired();
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.SnapshotSha256).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.HasIndex(x => x.VerificationCode).IsUnique();
        b.HasIndex(x => new { x.StudentId, x.IssuedAtUtc });
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
