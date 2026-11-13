using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Admissions.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using Xunit;
using Xunit.Sdk;

namespace UAF.Tests.Integration;

[Trait("Category", "SqlServerIntegration")]
public sealed class AdmissionWorkflowSqlServerIntegrationTests
{
    [Fact]
    public async Task ConcurrentConversionCreatesOneStudentAndConsumesOneRegistrationNumber()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configuredConnection))
            throw new SkipException("Set UAF_TEST_SQLSERVER_CONNECTION_STRING to run SQL Server integration tests.");

        var databaseName = $"UafAdmissionTests_{Guid.NewGuid():N}";
        var masterConnection = new SqlConnectionStringBuilder(configuredConnection) { InitialCatalog = "master" };
        var testConnection = new SqlConnectionStringBuilder(configuredConnection) { InitialCatalog = databaseName };

        await using (var connection = new SqlConnection(masterConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:UafDatabase"] = testConnection.ConnectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();

            Guid applicationId;
            Guid registrarId;
            var applicantId = Guid.NewGuid();
            var reviewerId = Guid.NewGuid();
            var decisionMakerId = Guid.NewGuid();
            var programId = Guid.NewGuid();

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                await db.Database.EnsureCreatedAsync();

                var university = new University("Integration Test University", "ITU");
                var faculty = new Faculty(university.Id, "Computing", "FC");
                var department = new Department(faculty.Id, "Computer Science", "CS");
                var program = new AcademicProgram(department.Id, "BS Computer Science", "BSCS",
                    DegreeLevel.Undergraduate, 4);
                programId = program.Id;

                var applicant = CreateUser(applicantId, "applicant");
                var reviewer = CreateUser(reviewerId, "reviewer");
                var decisionMaker = CreateUser(decisionMakerId, "decisionmaker");
                registrarId = Guid.NewGuid();
                var registrar = CreateUser(registrarId, "registrar");

                db.AddRange(university, faculty, department, program,
                    applicant, reviewer, decisionMaker, registrar);
                var application = new AdmissionApplication("UAF-IT-2026-001", "Integration Applicant",
                    "integration.applicant@example.com", program.Id, new DateOnly(2026, 11, 13),
                    ownerUserId: applicantId);
                application.Submit();
                applicationId = application.Id;
                db.AdmissionApplications.Add(application);
                await db.SaveChangesAsync();
            }

            using (var scope = provider.CreateScope())
            {
                var workflow = scope.ServiceProvider.GetRequiredService<IAdmissionWorkflowRepository>();
                await workflow.StartReviewAsync(applicationId, reviewerId, DateTimeOffset.UtcNow);
                await workflow.RecordDecisionAsync(applicationId, decisionMakerId,
                    AdmissionStatus.Accepted, "Integration test acceptance.", DateTimeOffset.UtcNow);
                await workflow.ConfigureRegistrationSeriesAsync(programId, 2026, "TEST-CS", 1,
                    registrarId);
            }

            async Task<Student> ConvertAsync()
            {
                using var scope = provider.CreateScope();
                var workflow = scope.ServiceProvider.GetRequiredService<IAdmissionWorkflowRepository>();
                return await workflow.ConvertAcceptedApplicationAsync(
                    applicationId, registrarId, DateTimeOffset.UtcNow);
            }

            var converted = await Task.WhenAll(ConvertAsync(), ConvertAsync());

            Assert.Equal(converted[0].Id, converted[1].Id);
            Assert.Equal("TEST-CS-2026-00001", converted[0].RegistrationNumber);

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                Assert.Equal(1, await db.Students.CountAsync(x => x.AdmissionApplicationId == applicationId));
                var series = await db.RegistrationNumberSeries.SingleAsync(
                    x => x.AcademicProgramId == programId && x.AdmissionYear == 2026);
                Assert.Equal(2, series.NextNumber);

                var history = await db.AdmissionDecisionAudits
                    .Where(x => x.ApplicationId == applicationId).ToListAsync();
                Assert.Equal(2, history.Count);
                Assert.Contains(history, x => x.FromStatus == AdmissionStatus.Submitted && x.ToStatus == AdmissionStatus.UnderReview);
                Assert.Contains(history, x => x.FromStatus == AdmissionStatus.UnderReview && x.ToStatus == AdmissionStatus.Accepted);
            }

            Assert.Equal(registrarId, converted[0].RegisteredByUserId);
            Assert.Equal(applicantId, converted[0].IdentityUserId);
        }
        finally
        {
            await using var connection = new SqlConnection(masterConnection.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                IF DB_ID(N'{databaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}];
                END
                """;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static ApplicationUser CreateUser(Guid id, string userName) => new()
    {
        Id = id,
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = $"{userName}@example.com",
        NormalizedEmail = $"{userName}@example.com".ToUpperInvariant(),
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        FullName = userName,
        IsActive = true
    };
}
