using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Integration;

[Trait("Category", "SqlServerIntegration")]
public sealed class GradeRevisionSqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task ApprovedGradeRevisionIsStoredAsImmutableSnapshotWithoutMutatingFinalizedGrade()
    {
        var configured = Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING")
            ?? throw new InvalidOperationException("SQL Server integration connection string was not configured.");
        var databaseName = $"UafGradeRevisionTests_{Guid.NewGuid():N}";
        var master = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master" };
        var target = new SqlConnectionStringBuilder(configured) { InitialCatalog = databaseName };

        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:UafDatabase"] = target.ConnectionString }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();

            var requesterId = Guid.NewGuid();
            var reviewerId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var gradeId = Guid.NewGuid();
            var appealId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                await db.Database.EnsureCreatedAsync();

                var university = new University("Grade Revision Integration University", "GRIU");
                var faculty = new Faculty(university.Id, "Computing", "COMP");
                var department = new Department(faculty.Id, "Computer Science", "CS");
                var program = new AcademicProgram(department.Id, "BS Computer Science", "BSCS", DegreeLevel.Undergraduate, 4);
                var session = new AcademicSession("Fall 2026", "FALL-2026", new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 31));
                var course = new Course(department.Id, "CS101", "Programming Fundamentals", 3);
                var offering = new CourseOffering(course.Id, session.Id, "A", 40);
                offering.Publish();
                var student = new Student("UAF-GR-001", "Grade Test Student", "student@grade.test",
                    program.Id, new DateOnly(2026, 9, 1), identityUserId: studentId);
                var scale = new GradeScale(session.Id, "Integration Scale", false, RepeatCoursePolicy.LatestAttempt, requesterId);
                var bandF = new GradeScaleBand(scale.Id, "F", 0, 0, false);
                var bandB = new GradeScaleBand(scale.Id, "B", 70, 3, true);
                var bandA = new GradeScaleBand(scale.Id, "A", 80, 4, true);
                scale.AddBand(bandF);
                scale.AddBand(bandB);
                scale.AddBand(bandA);
                scale.Publish(requesterId, now);

                var grade = new StudentCourseGrade(student.Id, offering.Id, course.Id, session.Id, scale.Id,
                    course.Code, course.Title, 3, 100, 72, 72, "B", 3, true, false,
                    RepeatCoursePolicy.LatestAttempt, requesterId, now);
                gradeId = grade.Id;
                var appeal = new GradeAppeal(grade.Id, student.Id, "Please recheck the final examination total.", now);
                appealId = appeal.Id;
                appeal.Decide(true, reviewerId, "Appeal accepted for re-evaluation.", now);

                db.AddRange(university, faculty, department, program, session, course, offering, student,
                    CreateUser(requesterId, "requester"), CreateUser(reviewerId, "reviewer"), scale,
                    bandF, bandB, bandA, grade, appeal);
                await db.SaveChangesAsync();
            }

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                var repository = scope.ServiceProvider.GetRequiredService<UAF.Application.Features.Grading.Abstractions.IGradeRevisionRepository>();
                var request = new GradeRevisionRequest(gradeId, appealId, 82, "Approved re-evaluation corrected the totaling error.", requesterId, now);
                await repository.AddRequestAsync(request, CancellationToken.None);
                requestId = request.Id;

                var revision = new GradeRevision(request.Id, gradeId, 1, 72, 82, 72, 82, "B", "A",
                    3, 4, true, true, request.Reason, requesterId, reviewerId, now.AddMinutes(1));
                var audit = new GradeRevisionAudit(request.Id, GradeRevisionStatus.PendingApproval,
                    GradeRevisionStatus.Approved, reviewerId, "Evidence independently checked.", now.AddMinutes(1));

                await repository.DecideRequestAsync(request.Id, reviewerId, true, audit.Remarks, revision,
                    audit, 0, CancellationToken.None);

                var persistedGrade = await db.StudentCourseGrades.SingleAsync(x => x.Id == gradeId);
                Assert.Equal(72m, persistedGrade.MarksObtained);
                Assert.Equal("B", persistedGrade.LetterGrade);

                var persistedRevision = await db.GradeRevisions.SingleAsync(x => x.StudentCourseGradeId == gradeId);
                Assert.Equal(72m, persistedRevision.PreviousMarks);
                Assert.Equal(82m, persistedRevision.NewMarks);
                Assert.Equal("A", persistedRevision.NewLetterGrade);

                var persistedRequest = await db.GradeRevisionRequests.SingleAsync(x => x.Id == requestId);
                Assert.Equal(GradeRevisionStatus.Approved, persistedRequest.Status);
                Assert.Single(await db.GradeRevisionAudits.Where(x => x.RequestId == requestId).ToListAsync());

                db.Entry(persistedRevision).State = EntityState.Modified;
                await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            }
        }
        finally
        {
            await using var connection = new SqlConnection(master.ConnectionString);
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

    private static ApplicationUser CreateUser(Guid id, string name) => new()
    {
        Id = id,
        UserName = name,
        NormalizedUserName = name.ToUpperInvariant(),
        Email = $"{name}@grade.test",
        NormalizedEmail = $"{name}@grade.test".ToUpperInvariant(),
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        FullName = name,
        IsActive = true
    };
}
