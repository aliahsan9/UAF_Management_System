using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application;
using UAF.Application.Features.Attendance;
using UAF.Application.Features.Attendance.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using Xunit;
namespace UAF.Tests.Integration;
[Trait("Category", "SqlServerIntegration")]
public sealed class AttendanceSqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task TeacherCanMarkCorrectAndFinalizeOnlyAssignedOfferingAttendance()
    {
        var configured = Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING")!;
        var name = $"UafAttendanceTests_{Guid.NewGuid():N}";
        var master = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master" };
        var target = new SqlConnectionStringBuilder(configured) { InitialCatalog = name };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{name}]";
            await command.ExecuteNonQueryAsync();
        }
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:UafDatabase"] = target.ConnectionString }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();

            var teacherId = Guid.NewGuid();
            var otherTeacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                await db.Database.EnsureCreatedAsync();
                var university = new University("Attendance Test University", "ATU");
                var faculty = new Faculty(university.Id, "Computing", "COMP");
                var department = new Department(faculty.Id, "Computer Science", "CS");
                var program = new AcademicProgram(department.Id, "BS Computer Science", "BSCS", DegreeLevel.Undergraduate, 4);
                var session = new AcademicSession("Fall 2026", "FALL-2026", new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 31));
                var course = new Course(department.Id, "CS101", "Programming Fundamentals", 3);
                var teacher = new Employee("EMP-ATT-001", "Test Teacher", "teacher@test.example", "Lecturer",
                    department.Id, EmployeeCategory.Academic, new DateOnly(2024, 1, 1), identityUserId: teacherId);
                var teacherProfile = new TeacherProfile(teacher.Id, "MS Computer Science", "Software Engineering", "Lecturer");
                var offering = new CourseOffering(course.Id, session.Id, "A", 40, teacher.Id);
                offering.Publish();
                offeringId = offering.Id;
                var student = new Student("ATT-TEST-001", "Test Student", "student@test.example", program.Id, new DateOnly(2026, 9, 1));
                studentId = student.Id;
                var enrollment = new CourseEnrollment(student.Id, offering.Id, new DateOnly(2026, 9, 1));
                db.AddRange(university, faculty, department, program, session, course, teacher, teacherProfile,
                    offering, student, enrollment, CreateUser(teacherId, "teacher"), CreateUser(otherTeacherId, "other"));
                await db.SaveChangesAsync();
            }

            Guid attendanceSessionId;
            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<AttendanceService>();
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateSessionAsync(
                    new CreateAttendanceSessionRequest(offeringId, new DateOnly(2026, 11, 13), "Lecture 1"),
                    otherTeacherId, false, CancellationToken.None));
                attendanceSessionId = await service.CreateSessionAsync(
                    new CreateAttendanceSessionRequest(offeringId, new DateOnly(2026, 11, 13), "Lecture 1"),
                    teacherId, false, CancellationToken.None);
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSessionAsync(
                    new CreateAttendanceSessionRequest(offeringId, new DateOnly(2026, 11, 13), "Duplicate"),
                    teacherId, false, CancellationToken.None));
                await service.MarkAsync(attendanceSessionId, teacherId, false,
                    new MarkAttendanceRequest(new[] { new AttendanceMarkItem(studentId, AttendanceStatus.Absent, null) }),
                    CancellationToken.None);
                await service.MarkAsync(attendanceSessionId, teacherId, false,
                    new MarkAttendanceRequest(new[] { new AttendanceMarkItem(studentId, AttendanceStatus.Present, "Student was present; initial mark corrected after roll call.") }),
                    CancellationToken.None);
                await service.FinalizeAsync(attendanceSessionId, teacherId, false, CancellationToken.None);
                var response = await service.GetAsync(attendanceSessionId, teacherId, false, CancellationToken.None);
                Assert.Equal(AttendanceSessionStatus.Finalized, response.Status);
                Assert.Equal(AttendanceStatus.Present, Assert.Single(response.Marks).Status);
            }

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                var history = await db.AttendanceChangeAudits.Where(x => x.AttendanceSessionId == attendanceSessionId)
                    .OrderBy(x => x.ChangedAtUtc).ToListAsync();
                Assert.Equal(2, history.Count);
                Assert.Null(history[0].PreviousStatus);
                Assert.Equal(AttendanceStatus.Absent, history[1].PreviousStatus);
                Assert.Equal(AttendanceStatus.Present, history[1].NewStatus);
                db.Entry(history[0]).State = EntityState.Modified;
                await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            }
        }
        finally
        {
            await using var connection = new SqlConnection(master.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static ApplicationUser CreateUser(Guid id, string name) => new()
    {
        Id = id, UserName = name, NormalizedUserName = name.ToUpperInvariant(),
        Email = $"{name}@test.example", NormalizedEmail = $"{name}@test.example".ToUpperInvariant(),
        EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"), FullName = name, IsActive = true
    };
}
