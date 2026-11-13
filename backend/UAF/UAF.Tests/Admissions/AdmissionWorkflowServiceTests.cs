using UAF.Application.Features.Admissions;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Admissions;

public sealed class AdmissionWorkflowServiceTests
{
    [Fact]
    public async Task RejectDecisionRequiresReason()
    {
        var repository = new FakeWorkflowRepository();
        var service = new AdmissionWorkflowService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordDecisionAsync(Guid.NewGuid(), Guid.NewGuid(), AdmissionStatus.Rejected, null));

        Assert.Equal(0, repository.DecisionCalls);
    }

    [Fact]
    public async Task DecisionRejectsUnsupportedStatus()
    {
        var service = new AdmissionWorkflowService(new FakeWorkflowRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.RecordDecisionAsync(Guid.NewGuid(), Guid.NewGuid(), AdmissionStatus.Draft, "No"));
    }

    [Fact]
    public async Task ConfigureRegistrationSeriesRequiresProgramAndPositiveSequence()
    {
        var service = new AdmissionWorkflowService(new FakeWorkflowRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ConfigureRegistrationSeriesAsync(
                new(Guid.Empty, 2026, "UAF", 1), Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ConfigureRegistrationSeriesAsync(
                new(Guid.NewGuid(), 2026, "UAF", 0), Guid.NewGuid()));
    }

    [Fact]
    public async Task ConversionMapsRegistrarAndStudentRegistrationDetails()
    {
        var repository = new FakeWorkflowRepository();
        var service = new AdmissionWorkflowService(repository);
        var actorId = Guid.NewGuid();

        var result = await service.ConvertAcceptedApplicationAsync(Guid.NewGuid(), actorId);

        Assert.Equal("UAF-2026-00001", result.RegistrationNumber);
        Assert.Equal(actorId, result.RegisteredByUserId);
        Assert.Equal(repository.Student.Id, result.StudentId);
        Assert.Equal(1, repository.ConversionCalls);
    }

    private sealed class FakeWorkflowRepository : IAdmissionWorkflowRepository
    {
        private readonly Guid _actorId = Guid.NewGuid();
        public int DecisionCalls { get; private set; }
        public int ConversionCalls { get; private set; }
        public Student Student { get; }

        public FakeWorkflowRepository()
        {
            Student = new Student("UAF-2026-00001", "Test Student", "student@example.com",
                Guid.NewGuid(), new DateOnly(2026, 11, 13), null, Guid.NewGuid(), Guid.NewGuid());
            Student.RecordAdmissionRegistration(_actorId, DateTimeOffset.UtcNow);
        }

        public Task StartReviewAsync(Guid applicationId, Guid actorUserId, DateTimeOffset occurredAtUtc, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordDecisionAsync(Guid applicationId, Guid actorUserId, AdmissionStatus decision, string? remarks, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
        {
            DecisionCalls++;
            return Task.CompletedTask;
        }

        public Task ConfigureRegistrationSeriesAsync(Guid academicProgramId, int admissionYear, string prefix, int nextNumber, Guid actorUserId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<Student> ConvertAcceptedApplicationAsync(Guid applicationId, Guid actorUserId, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
        {
            ConversionCalls++;
            return Task.FromResult(Student);
        }

        public Task<IReadOnlyList<AdmissionDecisionAudit>> GetDecisionHistoryAsync(Guid applicationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AdmissionDecisionAudit>>(Array.Empty<AdmissionDecisionAudit>());
    }
}
