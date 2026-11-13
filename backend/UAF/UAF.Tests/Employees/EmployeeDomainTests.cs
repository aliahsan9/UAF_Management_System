using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;
namespace UAF.Tests.Employees;
public sealed class EmployeeDomainTests
{
 private static Employee Create()=>new("EMP-001","Ali Example","ali@example.edu","Lecturer",Guid.NewGuid(),EmployeeCategory.Academic,new DateOnly(2024,1,1),"03001234567");
 [Fact] public void Employee_normalizes_employee_number_and_email(){var e=Create();Assert.Equal("EMP-001",e.EmployeeNumber);Assert.Equal("ali@example.edu",e.Email);}
 [Fact] public void Terminated_employee_cannot_be_reactivated_or_transferred(){var e=Create();e.ChangeStatus(EmployeeStatus.Terminated,new DateOnly(2026,1,1));Assert.Throws<InvalidOperationException>(()=>e.ChangeStatus(EmployeeStatus.Active));Assert.Throws<InvalidOperationException>(()=>e.TransferToDepartment(Guid.NewGuid()));}
 [Fact] public void Termination_date_cannot_precede_joining_date(){var e=Create();Assert.Throws<ArgumentException>(()=>e.ChangeStatus(EmployeeStatus.Terminated,new DateOnly(2023,12,31)));}
 [Fact] public void Identity_link_cannot_be_changed_after_initial_assignment(){var e=new Employee("EMP-002","A B","a@b.edu","Admin",Guid.NewGuid(),EmployeeCategory.Administrative,new DateOnly(2025,1,1),identityUserId:Guid.NewGuid());Assert.Throws<InvalidOperationException>(()=>e.LinkIdentity(Guid.NewGuid()));}
 [Fact] public void Teacher_profile_requires_valid_employee_and_qualification(){var p=new TeacherProfile(Guid.NewGuid(),"MS Computer Science","Artificial Intelligence","Lecturer");Assert.Equal("Artificial Intelligence",p.Specialization);Assert.Throws<ArgumentException>(()=>new TeacherProfile(Guid.NewGuid()," ","AI","Lecturer"));}
}