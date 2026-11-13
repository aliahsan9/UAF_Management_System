using System.Data;
using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

/// <summary>
/// Executes sensitive admission transitions and conversion inside serializable SQL Server transactions.
/// The application row lock makes decisions and conversion idempotent under concurrent requests.
/// </summary>
internal sealed class AdmissionWorkflowRepository : IAdmissionWorkflowRepository
{
    private readonly UafDbContext _db;
    public AdmissionWorkflowRepository(UafDbContext db) => _db = db;

    public async Task StartReviewAsync(Guid applicationId, Guid actorUserId, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var application = await GetApplicationForUpdateAsync(applicationId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");
        var from = application.Status;
        application.StartReview(actorUserId, occurredAtUtc);
        _db.AdmissionDecisionAudits.Add(new AdmissionDecisionAudit(
            application.Id, actorUserId, from, application.Status, null, occurredAtUtc));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task RecordDecisionAsync(Guid applicationId, Guid actorUserId, AdmissionStatus decision,
        string? remarks, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
    {
        if (decision is not AdmissionStatus.Accepted and not AdmissionStatus.Rejected and not AdmissionStatus.Waitlisted)
            throw new ArgumentOutOfRangeException(nameof(decision), "Unsupported admission decision.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var application = await GetApplicationForUpdateAsync(applicationId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");
        var from = application.Status;
        switch (decision)
        {
            case AdmissionStatus.Accepted: application.Accept(actorUserId, occurredAtUtc, remarks); break;
            case AdmissionStatus.Rejected: application.Reject(actorUserId, occurredAtUtc, remarks); break;
            case AdmissionStatus.Waitlisted: application.Waitlist(actorUserId, occurredAtUtc, remarks); break;
        }
        _db.AdmissionDecisionAudits.Add(new AdmissionDecisionAudit(
            application.Id, actorUserId, from, application.Status, remarks, occurredAtUtc));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ConfigureRegistrationSeriesAsync(Guid academicProgramId, int admissionYear,
        string prefix, int nextNumber, Guid actorUserId, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await _db.AcademicPrograms.AnyAsync(x => x.Id == academicProgramId && x.IsActive, ct))
            throw new KeyNotFoundException("An active academic program was not found.");

        var series = await _db.RegistrationNumberSeries.FromSqlInterpolated(
                $"SELECT * FROM [RegistrationNumberSeries] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademicProgramId] = {academicProgramId} AND [AdmissionYear] = {admissionYear}")
            .SingleOrDefaultAsync(ct);

        if (series is null)
            _db.RegistrationNumberSeries.Add(new RegistrationNumberSeries(
                academicProgramId, admissionYear, prefix, nextNumber, actorUserId));
        else
            series.Reconfigure(prefix, nextNumber, actorUserId);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<Student> ConvertAcceptedApplicationAsync(Guid applicationId, Guid actorUserId,
        DateTimeOffset occurredAtUtc, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var application = await GetApplicationForUpdateAsync(applicationId, ct)
            ?? throw new KeyNotFoundException("Admission application was not found.");

        if (application.Status != AdmissionStatus.Accepted)
            throw new InvalidOperationException("Only an accepted application can be converted into a student.");

        var existing = await _db.Students.SingleOrDefaultAsync(x => x.AdmissionApplicationId == applicationId, ct);
        if (existing is not null)
        {
            await transaction.CommitAsync(ct);
            return existing;
        }

        var year = application.ApplicationDate.Year;
        var series = await _db.RegistrationNumberSeries.FromSqlInterpolated(
                $"SELECT * FROM [RegistrationNumberSeries] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademicProgramId] = {application.AcademicProgramId} AND [AdmissionYear] = {year}")
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No registration-number series is configured for program {application.AcademicProgramId} and year {year}.");

        var registrationNumber = series.Allocate();
        var student = new Student(registrationNumber, application.ApplicantFullName,
            application.ApplicantEmail, application.AcademicProgramId,
            application.ApplicationDate, application.PhoneNumber,
            application.Id, application.OwnerUserId);
        student.RecordAdmissionRegistration(actorUserId, occurredAtUtc);
        _db.Students.Add(student);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return student;
    }

    public async Task<IReadOnlyList<AdmissionDecisionAudit>> GetDecisionHistoryAsync(Guid applicationId, CancellationToken ct = default)
    {
        if (!await _db.AdmissionApplications.AnyAsync(x => x.Id == applicationId, ct))
            throw new KeyNotFoundException("Admission application was not found.");
        return await _db.AdmissionDecisionAudits.AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .ToArrayAsync(ct);
    }

    private Task<AdmissionApplication?> GetApplicationForUpdateAsync(Guid applicationId, CancellationToken ct) =>
        _db.AdmissionApplications.FromSqlInterpolated(
                $"SELECT * FROM [AdmissionApplications] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {applicationId}")
            .SingleOrDefaultAsync(ct);
}
