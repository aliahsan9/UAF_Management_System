using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure.Persistence;

namespace UAF.Infrastructure.Identity;

/// <summary>
/// Loads a small University of Agriculture Faisalabad catalog when the development database is empty.
/// </summary>
public static class DevelopmentCatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UafDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevelopmentCatalogSeeder");

        if (await db.Universities.AnyAsync())
            return;

        var university = new University("University of Agriculture Faisalabad", "UAF");
        var campus = new Campus(university.Id, "Main Campus", "MAIN", "University Road, Faisalabad");
        var agriculture = new Faculty(university.Id, "Faculty of Agriculture", "AGR");
        var sciences = new Faculty(university.Id, "Faculty of Sciences", "SCI");
        var agronomy = new Department(agriculture.Id, "Department of Agronomy", "AGRON");
        var entomology = new Department(agriculture.Id, "Department of Entomology", "ENTO");
        var computerScience = new Department(sciences.Id, "Department of Computer Science", "CS");

        var bscAgriculture = new AcademicProgram(agronomy.Id, "BSc (Hons) Agriculture", "AGR-BSC", DegreeLevel.Undergraduate, 4);
        var mscAgronomy = new AcademicProgram(agronomy.Id, "MSc (Hons) Agronomy", "AGR-MSC", DegreeLevel.Graduate, 2);
        var bscEntomology = new AcademicProgram(entomology.Id, "BSc (Hons) Entomology", "ENT-BSC", DegreeLevel.Undergraduate, 4);
        var bsComputerScience = new AcademicProgram(computerScience.Id, "BS Computer Science", "CS-BS", DegreeLevel.Undergraduate, 4);

        var fall = new AcademicSession("Fall 2026", "FALL-2026", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 15));
        fall.Activate();
        var spring = new AcademicSession("Spring 2026", "SPR-2026", new DateOnly(2026, 2, 2), new DateOnly(2026, 6, 20));
        spring.Activate();
        spring.Complete();

        var courses = new[]
        {
            new Course(agronomy.Id, "AGR-101", "Introduction to Agronomy", 3),
            new Course(agronomy.Id, "AGR-204", "Crop Production", 3),
            new Course(entomology.Id, "ENT-110", "General Entomology", 3),
            new Course(computerScience.Id, "CS-201", "Data Structures", 4)
        };

        var enrolledOn = new DateOnly(2026, 9, 1);
        var students = new List<Student>
        {
            Student("2026-AGR-001", "Ayesha Khan", "ayesha.khan@uaf.edu.pk", bscAgriculture.Id, enrolledOn),
            Student("2026-AGR-002", "Hassan Ali", "hassan.ali@uaf.edu.pk", bscAgriculture.Id, enrolledOn),
            Student("2026-AGR-003", "Fatima Noor", "fatima.noor@uaf.edu.pk", bscAgriculture.Id, enrolledOn),
            Student("2026-AGR-004", "Usman Tariq", "usman.tariq@uaf.edu.pk", mscAgronomy.Id, enrolledOn),
            Student("2026-AGR-005", "Maryam Shah", "maryam.shah@uaf.edu.pk", mscAgronomy.Id, enrolledOn),
            Student("2026-ENT-001", "Bilal Ahmed", "bilal.ahmed@uaf.edu.pk", bscEntomology.Id, enrolledOn),
            Student("2026-ENT-002", "Sana Iqbal", "sana.iqbal@uaf.edu.pk", bscEntomology.Id, enrolledOn),
            Student("2026-ENT-003", "Hamza Raza", "hamza.raza@uaf.edu.pk", bscEntomology.Id, enrolledOn),
            Student("2026-CS-001", "Zainab Malik", "zainab.malik@uaf.edu.pk", bsComputerScience.Id, enrolledOn),
            Student("2026-CS-002", "Omar Farooq", "omar.farooq@uaf.edu.pk", bsComputerScience.Id, enrolledOn),
            Student("2026-CS-003", "Hira Javed", "hira.javed@uaf.edu.pk", bsComputerScience.Id, enrolledOn),
            Student("2026-CS-004", "Daniyal Khan", "daniyal.khan@uaf.edu.pk", bsComputerScience.Id, enrolledOn)
        };
        students[10].Suspend();

        var employees = new[]
        {
            new Employee("EMP-001", "Dr. Muhammad Asif", "asif@uaf.edu.pk", "Professor", agronomy.Id, EmployeeCategory.Academic, new DateOnly(2014, 3, 1)),
            new Employee("EMP-002", "Dr. Saima Batool", "saima.batool@uaf.edu.pk", "Associate Professor", entomology.Id, EmployeeCategory.Academic, new DateOnly(2016, 8, 15)),
            new Employee("EMP-003", "Imran Yousaf", "imran.yousaf@uaf.edu.pk", "Assistant Professor", computerScience.Id, EmployeeCategory.Academic, new DateOnly(2019, 1, 7)),
            new Employee("EMP-004", "Nadia Hussain", "nadia.hussain@uaf.edu.pk", "Registrar Clerk", agronomy.Id, EmployeeCategory.Administrative, new DateOnly(2018, 4, 2)),
            new Employee("EMP-005", "Kashif Mehmood", "kashif.mehmood@uaf.edu.pk", "Lab Technician", entomology.Id, EmployeeCategory.Technical, new DateOnly(2021, 11, 1)),
            new Employee("EMP-006", "Rabia Saleem", "rabia.saleem@uaf.edu.pk", "Student Affairs Officer", computerScience.Id, EmployeeCategory.Administrative, new DateOnly(2020, 6, 16))
        };

        db.Add(university);
        db.Add(campus);
        db.AddRange(agriculture, sciences);
        db.AddRange(agronomy, entomology, computerScience);
        db.AddRange(bscAgriculture, mscAgronomy, bscEntomology, bsComputerScience);
        db.AddRange(fall, spring);
        db.AddRange(courses);
        db.AddRange(students);
        db.AddRange(employees);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var recipient = await users.FindByEmailAsync("ali@gmail.com");
        if (recipient is not null)
        {
            db.Add(new Notification(recipient.Id, "Fall 2026 session is active", "Course registration for Fall 2026 is open for continuing students.", NotificationChannel.InApp));
            db.Add(new Notification(recipient.Id, "Catalog records are ready", "Students, employees, programs, and the active academic session are available in the portal.", NotificationChannel.InApp));
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded the development university catalog.");
    }

    private static Student Student(string registrationNumber, string fullName, string email, Guid programId, DateOnly enrollmentDate) =>
        new(registrationNumber, fullName, email, programId, enrollmentDate);
}
