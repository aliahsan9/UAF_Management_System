using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class AttendancePolicyConfiguration : IEntityTypeConfiguration<AttendancePolicy>
{
    public void Configure(EntityTypeBuilder<AttendancePolicy> b)
    {
        b.ToTable("AttendancePolicies", table =>
        {
            table.HasCheckConstraint("CK_AttendancePolicies_RequiredPercentage", "[RequiredPercentage] >= 0 AND [RequiredPercentage] <= 100");
            table.HasCheckConstraint("CK_AttendancePolicies_LateCreditPercentage", "[LateCreditPercentage] >= 0 AND [LateCreditPercentage] <= 100");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.RequiredPercentage).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.LateCreditPercentage).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.ExcludeExcusedFromDenominator).IsRequired();
        b.Property(x => x.ConfiguredAtUtc).IsRequired();
        b.HasIndex(x => x.AcademicSessionId).IsUnique();
        b.HasOne<AcademicSession>().WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfiguredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
