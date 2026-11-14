using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Common.Abstractions;
using UAF.Domain.Common;
using UAF.Domain.Entities;
using UAF.Infrastructure.Identity;

namespace UAF.Infrastructure.Persistence;

public sealed class UafDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public UafDbContext(DbContextOptions<UafDbContext> options) : base(options) { }

    public DbSet<University> Universities => Set<University>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<AcademicSession> AcademicSessions => Set<AcademicSession>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<AdmissionDocument> AdmissionDocuments => Set<AdmissionDocument>();
    public DbSet<AdmissionDocumentRequirement> AdmissionDocumentRequirements => Set<AdmissionDocumentRequirement>();
    public DbSet<AdmissionDecisionAudit> AdmissionDecisionAudits => Set<AdmissionDecisionAudit>();
    public DbSet<RegistrationNumberSeries> RegistrationNumberSeries => Set<RegistrationNumberSeries>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeStructureItem> FeeStructureItems => Set<FeeStructureItem>();
    public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
    public DbSet<FeeChallan> FeeChallans => Set<FeeChallan>();
    public DbSet<FeeChallanLine> FeeChallanLines => Set<FeeChallanLine>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<AttendanceChangeAudit> AttendanceChangeAudits => Set<AttendanceChangeAudit>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();
    public DbSet<Examination> Examinations => Set<Examination>();
    public DbSet<ExaminationResult> ExaminationResults => Set<ExaminationResult>();
    public DbSet<ExaminationResultAudit> ExaminationResultAudits => Set<ExaminationResultAudit>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<GradeScaleBand> GradeScaleBands => Set<GradeScaleBand>();
    public DbSet<StudentCourseGrade> StudentCourseGrades => Set<StudentCourseGrade>();
    public DbSet<GradeAppeal> GradeAppeals => Set<GradeAppeal>();
    public DbSet<GradeAppealAudit> GradeAppealAudits => Set<GradeAppealAudit>();
    public DbSet<GradeRevisionRequest> GradeRevisionRequests => Set<GradeRevisionRequest>();
    public DbSet<GradeRevision> GradeRevisions => Set<GradeRevision>();
    public DbSet<GradeRevisionAudit> GradeRevisionAudits => Set<GradeRevisionAudit>();
    public DbSet<AcademicTranscript> AcademicTranscripts => Set<AcademicTranscript>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<TeachingRoom> TeachingRooms => Set<TeachingRoom>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UafDbContext).Assembly);

        // Entity is a shared CLR base, not an EF inheritance root. Configure each
        // concrete persisted aggregate independently to avoid creating a synthetic base table.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => type.ClrType is not null &&
                                    typeof(Entity).IsAssignableFrom(type.ClrType) &&
                                    !type.ClrType.IsAbstract))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(Entity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Entity.RowVersion))
                .IsRowVersion();
        }

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.IsActive).IsRequired();
        });
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
        // Append-only and immutable invariants must be checked before delete conversion.
        if (ChangeTracker.Entries<AdmissionDecisionAudit>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Admission decision audit records are append-only.");
        if (ChangeTracker.Entries<AttendanceChangeAudit>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Attendance change audit records are append-only.");
        if (ChangeTracker.Entries<ExaminationResultAudit>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Examination result audit records are append-only.");
        if (ChangeTracker.Entries<GradeAppealAudit>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Grade appeal audit records are append-only.");
        if (ChangeTracker.Entries<GradeRevisionRequest>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Grade revision requests cannot be deleted.");
        if (ChangeTracker.Entries<GradeRevisionAudit>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Grade revision audit records are append-only.");
        if (ChangeTracker.Entries<GradeRevision>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Grade revision snapshots are immutable.");
        if (ChangeTracker.Entries<AcademicTranscript>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Issued academic transcripts are immutable; issue a new transcript for updated records.");
        if (ChangeTracker.Entries<StudentCourseGrade>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Finalized student course grades are immutable; use an authorized grade-revision workflow.");
        if (ChangeTracker.Entries<GradeScaleBand>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Grade scale bands are immutable after creation.");
        if (ChangeTracker.Entries<GradeScale>().Any(entry => entry.State == EntityState.Deleted || (entry.State == EntityState.Modified && entry.OriginalValues.GetValue<bool>(nameof(GradeScale.IsPublished)))))
            throw new InvalidOperationException("Published grading scales cannot be modified or deleted.");

        // Convert ordinary Remove() calls into updates so historical records remain available.
        foreach (var entry in ChangeTracker.Entries<Entity>()
                     .Where(entry => entry.State == EntityState.Deleted))
        {
            entry.Entity.SoftDelete();
            entry.State = EntityState.Modified;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(entity => entity.CreatedAtUtc).CurrentValue = now;
            if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = now;
            }
        }
    }
}
