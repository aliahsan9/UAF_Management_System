using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Admissions;

public sealed class AdmissionApplicationAuditTests
{
    [Fact]
    public void ReviewAndDecisionRecordAuthenticatedActorsAndUtcTimestamps()
    {
        var application = new AdmissionApplication("UAF-2026-100", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13));
        var reviewerId = Guid.NewGuid();
        var decisionMakerId = Guid.NewGuid();
        var reviewAt = new DateTimeOffset(2026, 11, 13, 8, 0, 0, TimeSpan.FromHours(5));
        var decisionAt = reviewAt.AddHours(2);

        application.Submit();
        application.StartReview(reviewerId, reviewAt);
        application.Accept(decisionMakerId, decisionAt, "Eligibility confirmed.");

        Assert.Equal(AdmissionStatus.Accepted, application.Status);
        Assert.Equal(reviewerId, application.ReviewStartedByUserId);
        Assert.Equal(reviewAt.ToUniversalTime(), application.ReviewStartedAtUtc);
        Assert.Equal(decisionMakerId, application.DecisionByUserId);
        Assert.Equal(decisionAt.ToUniversalTime(), application.DecisionAtUtc);
        Assert.Equal("Eligibility confirmed.", application.DecisionRemarks);
    }

    [Fact]
    public void RejectionRequiresReasonAndDoesNotChangeStateWhenValidationFails()
    {
        var application = new AdmissionApplication("UAF-2026-101", "Applicant", "applicant@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13));
        application.Submit();
        application.StartReview(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            application.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow, " "));

        Assert.Equal(AdmissionStatus.UnderReview, application.Status);
        Assert.Null(application.DecisionByUserId);
    }

    [Fact]
    public void RegistrationSeriesAllocatesMonotonicallyAndCannotMoveBackwards()
    {
        var series = new RegistrationNumberSeries(Guid.NewGuid(), 2026, "UAF-CS", 1, Guid.NewGuid());

        Assert.Equal("UAF-CS-2026-00001", series.Allocate());
        Assert.Equal("UAF-CS-2026-00002", series.Allocate());
        Assert.Throws<InvalidOperationException>(() => series.Reconfigure("UAF-CS", 1, Guid.NewGuid()));
    }
}
