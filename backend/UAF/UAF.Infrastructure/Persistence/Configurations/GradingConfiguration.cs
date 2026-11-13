using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
{
    public void Configure(EntityTypeBuilder<GradeScale> b)
    {
        b.ToTable("GradeScales", t =>
        {
            t.HasCheckConstraint("CK_GradeScales_RepeatCoursePolicy", "[RepeatCoursePolicy] IN (1, 2, 3)");
        });
        b.HasKey(x => x.Id);
        b.Ignore(x => x.Bands);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.RepeatCoursePolicy).HasConversion<int>().IsRequired();
        b.Property(x => x.IncludeFailedCoursesInGpa).IsRequired();
        b.Property(x => x.IsPublished).IsRequired();
        b.HasIndex(x => x.AcademicSessionId).IsUnique();
        b.HasOne<AcademicSession>().WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfiguredByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany<GradeScaleBand>("_bands").WithOne().HasForeignKey(x => x.GradeScaleId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation("_bands").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class GradeScaleBandConfiguration : IEntityTypeConfiguration<GradeScaleBand>
{
    public void Configure(EntityTypeBuilder<GradeScaleBand> b)
    {
        b.ToTable("GradeScaleBands", t =>
        {
            t.HasCheckConstraint("CK_GradeScaleBands_MinimumPercentage", "[MinimumPercentage] >= 0 AND [MinimumPercentage] <= 100");
            t.HasCheckConstraint("CK_GradeScaleBands_GradePoints", "[GradePoints] >= 0 AND [GradePoints] <= 10");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.LetterGrade).HasMaxLength(10).IsRequired();
        b.Property(x => x.MinimumPercentage).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.GradePoints).HasPrecision(4, 2).IsRequired();
        b.HasIndex(x => new { x.GradeScaleId, x.LetterGrade }).IsUnique();
        b.HasIndex(x => new { x.GradeScaleId, x.MinimumPercentage }).IsUnique();
    }
}
public sealed class StudentCourseGradeConfiguration : IEntityTypeConfiguration<StudentCourseGrade>
{
    public void Configure(EntityTypeBuilder<StudentCourseGrade> b)
    {
        b.ToTable("StudentCourseGrades", t =>
        {
            t.HasCheckConstraint("CK_StudentCourseGrades_Marks", "[TotalMarks] > 0 AND [MarksObtained] >= 0 AND [MarksObtained] <= [TotalMarks]");
            t.HasCheckConstraint("CK_StudentCourseGrades_Percentage", "[Percentage] >= 0 AND [Percentage] <= 100");
            t.HasCheckConstraint("CK_StudentCourseGrades_CreditHours", "[CreditHours] > 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.CourseCode).HasMaxLength(30).IsRequired();
        b.Property(x => x.CourseTitle).HasMaxLength(180).IsRequired();
        b.Property(x => x.CreditHours).HasPrecision(4, 2).IsRequired();
        b.Property(x => x.TotalMarks).HasPrecision(10, 2).IsRequired();
        b.Property(x => x.MarksObtained).HasPrecision(10, 2).IsRequired();
        b.Property(x => x.Percentage).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.LetterGrade).HasMaxLength(10).IsRequired();
        b.Property(x => x.GradePoints).HasPrecision(4, 2).IsRequired();
        b.Property(x => x.RepeatCoursePolicy).HasConversion<int>().IsRequired();
        b.HasIndex(x => new { x.StudentId, x.CourseOfferingId }).IsUnique();
        b.HasIndex(x => new { x.StudentId, x.AcademicSessionId });
        b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AcademicSession>().WithMany().HasForeignKey(x => x.AcademicSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<GradeScale>().WithMany().HasForeignKey(x => x.GradeScaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
