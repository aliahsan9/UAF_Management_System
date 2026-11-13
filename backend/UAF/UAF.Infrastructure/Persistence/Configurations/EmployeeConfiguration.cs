using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;
namespace UAF.Infrastructure.Persistence.Configurations;
public sealed class EmployeeConfiguration:IEntityTypeConfiguration<Employee>
{
 public void Configure(EntityTypeBuilder<Employee>b)
 {
  b.ToTable("Employees",t=>{t.HasCheckConstraint("CK_Employees_TerminationDate","([Status] <> 4 AND [TerminationDate] IS NULL) OR ([Status] = 4 AND [TerminationDate] IS NOT NULL)");});
  b.HasKey(x=>x.Id);b.Property(x=>x.EmployeeNumber).HasMaxLength(40).IsRequired();b.Property(x=>x.FullName).HasMaxLength(180).IsRequired();b.Property(x=>x.Email).HasMaxLength(254).IsRequired();b.Property(x=>x.Designation).HasMaxLength(120).IsRequired();b.Property(x=>x.PhoneNumber).HasMaxLength(30);b.Property(x=>x.Category).HasConversion<int>().IsRequired();b.Property(x=>x.Status).HasConversion<int>().IsRequired();
  b.HasIndex(x=>x.EmployeeNumber).IsUnique();b.HasIndex(x=>x.Email);b.HasIndex(x=>x.DepartmentId);b.HasIndex(x=>x.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
  b.HasOne<Department>().WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.IdentityUserId).OnDelete(DeleteBehavior.Restrict);
 }
}
public sealed class TeacherProfileConfiguration:IEntityTypeConfiguration<TeacherProfile>
{
 public void Configure(EntityTypeBuilder<TeacherProfile>b)
 {b.ToTable("TeacherProfiles");b.HasKey(x=>x.Id);b.Property(x=>x.HighestQualification).HasMaxLength(120).IsRequired();b.Property(x=>x.Specialization).HasMaxLength(160).IsRequired();b.Property(x=>x.AcademicRank).HasMaxLength(100).IsRequired();b.HasIndex(x=>x.EmployeeId).IsUnique();b.HasOne<Employee>().WithOne().HasForeignKey<TeacherProfile>(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);}
}