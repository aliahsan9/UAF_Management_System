using Microsoft.EntityFrameworkCore;
using UAF.Application.Common.Abstractions;
using UAF.Domain.Common;
using UAF.Domain.Entities;

namespace UAF.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the UAF management system.
/// The context also implements the application's Unit of Work abstraction.
/// </summary>
public sealed class UafDbContext : DbContext, IUnitOfWork
{
    public UafDbContext(DbContextOptions<UafDbContext> options)
        : base(options)
    {
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<AcademicSession> AcademicSessions => Set<AcademicSession>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UafDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = now;
            }
        }
    }
}
