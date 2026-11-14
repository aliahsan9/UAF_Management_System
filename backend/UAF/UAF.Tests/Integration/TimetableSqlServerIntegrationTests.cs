using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application;
using UAF.Application.Features.CourseOfferings.Abstractions;
using UAF.Application.Features.Timetable;
using UAF.Application.Features.Timetable.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Integration;

[Trait("Category", "SqlServerIntegration")]
public sealed class TimetableSqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task SchedulingAndEnrollmentRejectRoomInstructorAndStudentConflicts()
    {
        var configured = Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING")!;
        var name = $"UafTimetableTests_{Guid.NewGuid():N}";
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

            Guid offeringOneId;
            Guid offeringTwoId;
            Guid offeringThreeId;
            Guid studentId;
            Guid roomOneId;
            Guid roomTwoId;
            Guid roomThreeId;
            var sessionStart = new DateOnly(2026, 8, 1);
            var sessionEnd = new DateOnly(2026, 12, 31);
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
                await db.Database.EnsureCreatedAsync();

                var university = new University("Timetable Test University", "TTU");
                var campus = new Campus(university.Id, "Main Campus", "MAIN");
                var faculty = new Faculty(university.Id, "Faculty of Computing", "FC");
                var department = new Department(faculty.Id, "Computer Science", "CS");
                var program = new AcademicProgram(department.Id, "BS Computer Science", "BSCS", DegreeLevel.Undergraduate, 4);
                var session = new AcademicSession("Fall 2026", "FALL-2026", sessionStart, sessionEnd);
                var teacherOne = new Employee("TT-EMP-001", "Teacher One", "teacher1@test.example", "Lecturer",
                    department.Id, EmployeeCategory.Academic, new DateOnly(2023, 1, 1));
                var teacherTwo = new Employee("TT-EMP-002", "Teacher Two", "teacher2@test.example", "Lecturer",
                    department.Id, EmployeeCategory.Academic, new DateOnly(2023, 1, 1));
                var courseOne = new Course(department.Id, "TT101", "Course One", 3);
                var courseTwo = new Course(department.Id, "TT102", "Course Two", 3);
                var courseThree = new Course(department.Id, "TT103", "Course Three", 3);
                var offeringOne = new CourseOffering(courseOne.Id, session.Id, "A", 40, teacherOne.Id);
                var offeringTwo = new CourseOffering(courseTwo.Id, session.Id, "A", 40, teacherTwo.Id);
                var offeringThree = new CourseOffering(courseThree.Id, session.Id, "A", 40, teacherTwo.Id);
                offeringOne.Publish();
                offeringTwo.Publish();
                offeringThree.Publish();
                var student = new Student("TT-STU-001", "Test Student", "ttstudent@test.example", program.Id, new DateOnly(2026, 9, 1));
                db.AddRange(university, campus, faculty, department, program, session, teacherOne, teacherTwo,
                    courseOne, courseTwo, courseThree, offeringOne, offeringTwo, offeringThree, student,
                    new CourseEnrollment(student.Id, offeringOne.Id, new DateOnly(2026, 9, 1)));
                await db.SaveChangesAsync();

                offeringOneId = offeringOne.Id;
                offeringTwoId = offeringTwo.Id;
                offeringThreeId = offeringThree.Id;
                studentId = student.Id;
            }

            using (var scope = provider.CreateScope())
            {
                var timetable = scope.ServiceProvider.GetRequiredService<TimetableService>();
                roomOneId = await timetable.CreateRoomAsync(new CreateTeachingRoomRequest(
                    GetCampusId(provider, target.ConnectionString), "R-101", "Room 101", 60, "Classroom"), CancellationToken.None);
                roomTwoId = await timetable.CreateRoomAsync(new CreateTeachingRoomRequest(
                    GetCampusId(provider, target.ConnectionString), "R-102", "Room 102", 60, "Classroom"), CancellationToken.None);
                roomThreeId = await timetable.CreateRoomAsync(new CreateTeachingRoomRequest(
                    GetCampusId(provider, target.ConnectionString), "R-103", "Room 103", 60, "Classroom"), CancellationToken.None);

                var monday = new DateOnly(2026, 9, 7);
                await timetable.ScheduleAsync(new CreateTimetableEntryRequest(offeringOneId, roomOneId,
                    DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), monday, sessionEnd), CancellationToken.None);

                await Assert.ThrowsAsync<InvalidOperationException>(() => timetable.ScheduleAsync(
                    new CreateTimetableEntryRequest(offeringTwoId, roomOneId, DayOfWeek.Monday,
                        new TimeOnly(9, 30), new TimeOnly(10, 30), monday, sessionEnd), CancellationToken.None));

                // Different room/instructor is allowed initially because the second offering
                // has no enrolled students yet. The registration operation must enforce the
                // student's conflict before creating the enrollment.
                await timetable.ScheduleAsync(new CreateTimetableEntryRequest(offeringTwoId, roomTwoId,
                    DayOfWeek.Monday, new TimeOnly(9, 30), new TimeOnly(10, 30), monday, sessionEnd), CancellationToken.None);

                var enrollments = scope.ServiceProvider.GetRequiredService<ICourseOfferingRepository>();
                await Assert.ThrowsAsync<InvalidOperationException>(() => enrollments.EnrollAsync(
                    studentId, offeringTwoId, new DateOnly(2026, 9, 1), CancellationToken.None));

                await Assert.ThrowsAsync<InvalidOperationException>(() => timetable.ScheduleAsync(
                    new CreateTimetableEntryRequest(offeringThreeId, roomThreeId, DayOfWeek.Monday,
                        new TimeOnly(9, 45), new TimeOnly(10, 45), monday, sessionEnd), CancellationToken.None));
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

    private static Guid GetCampusId(IServiceProvider provider, string connectionString)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
        return db.Campuses.AsNoTracking().Select(x => x.Id).Single();
    }
}
