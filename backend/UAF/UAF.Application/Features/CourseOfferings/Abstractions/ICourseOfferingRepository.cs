using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.CourseOfferings.Abstractions;

public interface ICourseOfferingRepository
{
    Task<IReadOnlyList<CourseOffering>> GetOfferingsAsync(Guid? academicSessionId, Guid? courseId, CourseOfferingStatus? status, CancellationToken ct = default);
    Task<CourseOffering?> GetOfferingByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> CourseIsActiveAsync(Guid courseId, CancellationToken ct = default);
    Task<bool> AcademicSessionAllowsSchedulingAsync(Guid sessionId, CancellationToken ct = default);
    Task<bool> OfferingSectionExistsAsync(Guid courseId, Guid sessionId, string section, Guid? excludingId = null, CancellationToken ct = default);
    Task<int> GetActiveEnrollmentCountAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> GetActiveEnrollmentCountsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default);
    Task AddOfferingAsync(CourseOffering offering, CancellationToken ct = default);
    Task UpdateOfferingAsync(Guid offeringId, int capacity, Guid? instructorEmployeeId, CancellationToken ct = default);
    Task<IReadOnlyList<CourseEnrollment>> GetEnrollmentsAsync(Guid offeringId, CancellationToken ct = default);
    Task<CourseEnrollment?> GetEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<CourseEnrollment> EnrollAsync(Guid studentId, Guid offeringId, DateOnly enrolledOn, CancellationToken ct = default);
}