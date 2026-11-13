using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Transcripts.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Integration;

[Trait("Category", "SqlServerIntegration")]
public sealed class TranscriptSqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task TranscriptSnapshotIsPersistedReadOnlyAndVerificationCodeIsUnique()
    {
        var configured = Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING")
            ?? throw new InvalidOperationException("SQL Server integration connection string was not configured.");
        var databaseName = $"UafTranscriptTests_{Guid.NewGuid():N}";
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
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:UafDatabase"] = target.ConnectionString
                }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();

            var identityId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var snapshot = "{\"registrationNumber\":\"UAF-IT-001\",\"gpa\":3.75}";
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
            var issuedAt = DateTimeOffset.UtcNow;

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                await db.Database.EnsureCreatedAsync();

                var university = new University("Transcript Integration University", "TIU");
                var faculty = new Faculty(university.Id, "Computing", "COMP");
                var department = new Department(faculty.Id, "Computer Science", "CS");
                var program = new AcademicProgram(department.Id, "BS Computer Science", "BSCS",
                    DegreeLevel.Undergraduate, 4);
                var student = new Student("UAF-IT-001", "Integration Student", "student@transcript.test",
                    program.Id, new DateOnly(2026, 9, 1), identityUserId: identityId);
                studentId = student.Id;

                db.AddRange(university, faculty, department, program, student,
                    CreateUser(identityId, "student"), CreateUser(registrarId, "registrar"));
                await db.SaveChangesAsync();
            }

            using (var scope = provider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ITranscriptRepository>();
                var transcript = new AcademicTranscript(studentId, registrarId, code, snapshot, digest, issuedAt);
                await repository.AddAsync(transcript, CancellationToken.None);

                var persisted = await repository.GetByVerificationCodeAsync(code, CancellationToken.None);
                Assert.NotNull(persisted);
                Assert.Equal(snapshot, persisted.SnapshotJson);
                Assert.Equal(digest, persisted.SnapshotSha256);

                var listed = await repository.GetForStudentAsync(studentId, CancellationToken.None);
                var item = Assert.Single(listed);
                Assert.Equal(transcript.Id, item.Id);

                var owned = await repository.GetForStudentAsync(transcript.Id, studentId, CancellationToken.None);
                Assert.NotNull(owned);
                Assert.Null(await repository.GetForStudentAsync(transcript.Id, Guid.NewGuid(), CancellationToken.None));

                var duplicate = new AcademicTranscript(studentId, registrarId, code, snapshot, digest, issuedAt.AddMinutes(1));
                await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(duplicate, CancellationToken.None));
            }

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                var persisted = await db.AcademicTranscripts.SingleAsync(x => x.VerificationCode == code);
                db.Entry(persisted).State = EntityState.Modified;
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
        Email = $"{name}@transcript.test",
        NormalizedEmail = $"{name}@transcript.test".ToUpperInvariant(),
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        FullName = name,
        IsActive = true
    };
}
