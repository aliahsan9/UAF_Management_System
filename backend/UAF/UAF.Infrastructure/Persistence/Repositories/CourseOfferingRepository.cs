using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.CourseOfferings.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class CourseOfferingRepository : ICourseOfferingRepository
{
    private readonly UafDbContext _dbContext;

    public CourseOfferingRepository(UafDbContext dbContext) =>
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyList<CourseOffering>> GetOfferingsAsync(Guid? academicSessionId, Guid? courseId, CourseOfferingStatus? status, CancellationToken ct = default) =>
        await _dbContext.CourseOfferings.AsNoTracking()
            .Where(x => (!academicSessionId.HasValue || x.AcademicSessionId == academicSessionId.Value) &&
                        (!courseId.HasValue || x.CourseId == courseId.Value) &&
                        (!status.HasValue || x.Status == status.Value))
            .OrderBy(x => x.AcademicSessionId).ThenBy(x => x.CourseId).ThenBy(x => x.Section)
            .ToListAsync(ct);

    public Task<CourseOffering?> GetOfferingByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.CourseOfferings.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> CourseIsActiveAsync(Guid courseId, CancellationToken ct = default) =>
        _dbContext.Courses.AnyAsync(x => x.Id == courseId && x.IsActive, ct);

    public Task<bool> AcademicSessionAllowsSchedulingAsync(Guid sessionId, CancellationToken ct = default) =>
        _dbContext.AcademicSessions.AnyAsync(x => x.Id == sessionId &&
            (x.Status == AcademicSessionStatus.Planned || x.Status == AcademicSessionStatus.Active), ct);

    public Task<bool> OfferingSectionExistsAsync(Guid courseId, Guid sessionId, string section, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.CourseOfferings.AnyAsync(x => x.CourseId == courseId && x.AcademicSessionId == sessionId &&
            x.Section == section && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<int> GetActiveEnrollmentCountAsync(Guid offeringId, CancellationToken ct = default) =>
        _dbContext.CourseEnrollments.CountAsync(x => x.CourseOfferingId == offeringId && x.Status == EnrollmentStatus.Enrolled, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetActiveEnrollmentCountsAsync(
        IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default)
    {
        if (offeringIds.Count == 0) return new Dictionary<Guid, int>();
        return await _dbContext.CourseEnrollments.AsNoTracking()
            .Where(x => offeringIds.Contains(x.CourseOfferingId) && x.Status == EnrollmentStatus.Enrolled)
            .GroupBy(x => x.CourseOfferingId)
            .Select(group => new { OfferingId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.OfferingId, x => x.Count, ct);
    }

    public Task AddOfferingAsync(CourseOffering offering, CancellationToken ct = default) =>
        _dbContext.CourseOfferings.AddAsync(offering, ct).AsTask();

    public async Task<IReadOnlyList<CourseEnrollment>> GetEnrollmentsAsync(Guid offeringId, CancellationToken ct = default) =>
        await _dbContext.CourseEnrollments.AsNoTracking().Where(x => x.CourseOfferingId == offeringId)
            .OrderBy(x => x.EnrolledOn).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<CourseEnrollment?> GetEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default) =>
        _dbContext.CourseEnrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId, ct);


    private Task<CourseOffering?> GetOfferingForUpdateAsync(Guid offeringId, CancellationToken ct) =>
        _dbContext.CourseOfferings
            .FromSqlInterpolated($"SELECT * FROM [CourseOfferings] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {offeringId}")
            .FirstOrDefaultAsync(ct);

    public async Task UpdateOfferingAsync(Guid offeringId, int capacity, Guid? instructorEmployeeId, CancellationToken ct = default)
    {
        if (instructorEmployeeId == Guid.Empty)
            throw new ArgumentException("Instructor employee ID cannot be an empty GUID.", nameof(instructorEmployeeId));

        // Serialize capacity changes with enrollment transactions so seats cannot be over-allocated.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var offering = await GetOfferingForUpdateAsync(offeringId, ct)
            ?? throw new KeyNotFoundException($"Course offering '{offeringId}' was not found.");
        var enrolledCount = await _dbContext.CourseEnrollments.CountAsync(
            x => x.CourseOfferingId == offeringId && x.Status == EnrollmentStatus.Enrolled, ct);
        if (capacity < enrolledCount)
            throw new InvalidOperationException($"Capacity cannot be lower than the {enrolledCount} currently enrolled students.");

        offering.UpdateCapacity(capacity);
        offering.AssignInstructor(instructorEmployeeId);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<CourseEnrollment> EnrollAsync(Guid studentId, Guid offeringId, DateOnly enrolledOn, CancellationToken ct = default)
    {
        // Serializable isolation protects the seat-count predicate from concurrent inserts.
        // The unique (StudentId, CourseOfferingId) index independently prevents duplicate registrations.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var offering = await GetOfferingForUpdateAsync(offeringId, ct)
            ?? throw new KeyNotFoundException($"Course offering '{offeringId}' was not found.");
        if (offering.Status != CourseOfferingStatus.Published)
            throw new InvalidOperationException("The course offering is no longer open for enrollment.");

        var sessionAllowsEnrollment = await _dbContext.AcademicSessions.AnyAsync(
            x => x.Id == offering.AcademicSessionId &&
                (x.Status == AcademicSessionStatus.Planned || x.Status == AcademicSessionStatus.Active), ct);
        if (!sessionAllowsEnrollment)
            throw new InvalidOperationException("Enrollment is not available for a completed or cancelled academic session.");

        var duplicate = await _dbContext.CourseEnrollments.AnyAsync(
            x => x.StudentId == studentId && x.CourseOfferingId == offeringId &&
                (x.Status == EnrollmentStatus.Enrolled || x.Status == EnrollmentStatus.Completed), ct);
        if (duplicate)
            throw new InvalidOperationException("This student already has an enrollment record for this course offering.");

        var enrolledCount = await _dbContext.CourseEnrollments.CountAsync(
            x => x.CourseOfferingId == offeringId && x.Status == EnrollmentStatus.Enrolled, ct);
        if (enrolledCount >= offering.Capacity)
            throw new InvalidOperationException("The course offering has reached its enrollment capacity.");

        var studentIsActive = await _dbContext.Students.AnyAsync(
            x => x.Id == studentId && x.Status == StudentStatus.Active, ct);
        if (!studentIsActive)
            throw new InvalidOperationException("The student is not active and eligible for enrollment.");

        // A student must not be registered into an offering whose published timetable
        // overlaps any of their already-enrolled offerings in the same recurring week.
        var timetableConflict =
            from newSlot in _dbContext.TimetableEntries
            from existingEnrollment in _dbContext.CourseEnrollments
            join existingSlot in _dbContext.TimetableEntries
                on existingEnrollment.CourseOfferingId equals existingSlot.CourseOfferingId
            where newSlot.CourseOfferingId == offeringId &&
                  newSlot.Status == TimetableEntryStatus.Published &&
                  existingEnrollment.StudentId == studentId &&
                  existingEnrollment.Status == EnrollmentStatus.Enrolled &&
                  existingSlot.Status == TimetableEntryStatus.Published &&
                  newSlot.DayOfWeek == existingSlot.DayOfWeek &&
                  newSlot.EffectiveFrom <= existingSlot.EffectiveTo &&
                  existingSlot.EffectiveFrom <= newSlot.EffectiveTo &&
                  newSlot.StartTime < existingSlot.EndTime &&
                  existingSlot.StartTime < newSlot.EndTime
            select existingSlot.Id;
        if (await timetableConflict.AnyAsync(ct))
            throw new InvalidOperationException("The student's timetable conflicts with another active course enrollment.");

        var enrollment = new CourseEnrollment(studentId, offeringId, enrolledOn);
        _dbContext.CourseEnrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return enrollment;
    }
}