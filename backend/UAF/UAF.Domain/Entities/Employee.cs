using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>University employee record. Authentication identity and authorization roles are managed separately.</summary>
public sealed class Employee : AuditableEntity
{
 private Employee(){EmployeeNumber=FullName=Email=Designation=string.Empty;}
 public Employee(string employeeNumber,string fullName,string email,string designation,Guid departmentId,EmployeeCategory category,DateOnly joiningDate,string? phoneNumber=null,Guid? identityUserId=null)
 {
  EmployeeNumber=Required(employeeNumber,40,nameof(employeeNumber)).ToUpperInvariant();
  FullName=Required(fullName,180,nameof(fullName));Email=EmailValue(email);Designation=Required(designation,120,nameof(designation));
  if(departmentId==Guid.Empty)throw new ArgumentException("Department is required.",nameof(departmentId));
  if(identityUserId==Guid.Empty)throw new ArgumentException("Identity user ID cannot be empty.",nameof(identityUserId));
  if(!Enum.IsDefined(category))throw new ArgumentOutOfRangeException(nameof(category));
  DepartmentId=departmentId;Category=category;JoiningDate=joiningDate;PhoneNumber=Optional(phoneNumber,30);IdentityUserId=identityUserId;Status=EmployeeStatus.Active;
 }
 public string EmployeeNumber{get;private set;}
 public string FullName{get;private set;}
 public string Email{get;private set;}
 public string Designation{get;private set;}
 public string? PhoneNumber{get;private set;}
 public Guid DepartmentId{get;private set;}
 public Guid? IdentityUserId{get;private set;}
 public EmployeeCategory Category{get;private set;}
 public DateOnly JoiningDate{get;private set;}
 public DateOnly? TerminationDate{get;private set;}
 public EmployeeStatus Status{get;private set;}
 public void UpdateProfile(string fullName,string email,string designation,string? phoneNumber)
 {FullName=Required(fullName,180,nameof(fullName));Email=EmailValue(email);Designation=Required(designation,120,nameof(designation));PhoneNumber=Optional(phoneNumber,30);MarkUpdated();}
 public void TransferToDepartment(Guid departmentId)
 {if(departmentId==Guid.Empty)throw new ArgumentException("Department is required.");if(Status==EmployeeStatus.Terminated)throw new InvalidOperationException("Terminated employee cannot be transferred.");DepartmentId=departmentId;MarkUpdated();}
 public void LinkIdentity(Guid userId)
 {if(userId==Guid.Empty)throw new ArgumentException("Identity user is required.");if(IdentityUserId.HasValue&&IdentityUserId!=userId)throw new InvalidOperationException("Employee is already linked to another identity.");IdentityUserId=userId;MarkUpdated();}
 public void ChangeStatus(EmployeeStatus status,DateOnly? effectiveDate=null)
 {
  if(!Enum.IsDefined(status))throw new ArgumentOutOfRangeException(nameof(status));
  if(Status==EmployeeStatus.Terminated)throw new InvalidOperationException("Terminated employee record is immutable.");
  if(status==EmployeeStatus.Terminated){var date=effectiveDate??DateOnly.FromDateTime(DateTime.UtcNow);if(date<JoiningDate)throw new ArgumentException("Termination date cannot precede joining date.");TerminationDate=date;}
  else if(TerminationDate.HasValue)throw new InvalidOperationException("A terminated employee cannot be reactivated; create a new employment record.");
  Status=status;MarkUpdated();
 }
 private static string Required(string v,int max,string p){if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max)throw new ArgumentException($"A value up to {max} characters is required.",p);return v.Trim();}
 private static string EmailValue(string v){var x=Required(v,254,nameof(v)).ToLowerInvariant();if(!x.Contains('@'))throw new ArgumentException("A valid email is required.");return x;}
 private static string? Optional(string? v,int max){if(string.IsNullOrWhiteSpace(v))return null;if(v.Trim().Length>max)throw new ArgumentException($"Value cannot exceed {max} characters.");return v.Trim();}
}