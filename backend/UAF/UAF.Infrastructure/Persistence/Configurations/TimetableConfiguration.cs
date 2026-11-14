using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence.Configurations;

public sealed class TeachingRoomConfiguration : IEntityTypeConfiguration<TeachingRoom>
{
    public void Configure(EntityTypeBuilder<TeachingRoom> builder)
    {
        builder.ToTable("TeachingRooms", table =>
        {
            table.HasCheckConstraint("CK_TeachingRooms_Capacity", "[Capacity] > 0 AND [Capacity] <= 5000");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RoomType).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.CampusId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.CampusId, x.IsActive });
        builder.HasOne<Campus>().WithMany().HasForeignKey(x => x.CampusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> builder)
    {
        builder.ToTable("TimetableEntries", table =>
        {
            table.HasCheckConstraint("CK_TimetableEntries_TimeRange", "[EndTime] > [StartTime]");
            table.HasCheckConstraint("CK_TimetableEntries_DateRange", "[EffectiveTo] >= [EffectiveFrom]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time(0)").IsRequired();
        builder.HasIndex(x => new { x.RoomId, x.DayOfWeek, x.Status, x.StartTime, x.EndTime });
        builder.HasIndex(x => new { x.CourseOfferingId, x.DayOfWeek, x.Status });
        builder.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeachingRoom>().WithMany().HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
