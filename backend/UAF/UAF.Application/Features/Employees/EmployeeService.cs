using UAF.Application.Features.Employees.Abstractions;
using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Employees.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Employees;
public sealed class EmployeeService
{
 private const int MaxPageSize=100;private readonly IEmployeeRepository repo;private readonly IUnitOfWork unitOfWork;
 public EmployeeService(IEmployeeRepository repo,IUnitOfWork unitOfWork){this.repo=repo??throw new ArgumentNullException(nameof(repo));this.unitOfWork=unitOfWork??throw new ArgumentNullException(nameof(unitOfWork));}
 public async Task<PagedEmployeesResponse> GetPageAsync(int page,int pageSize,Guid? departmentId,EmployeeStatus? status,EmployeeCategory? category,CancellationToken ct)
 {
  if(page<1||pageSize is <1 or >MaxPageSize)throw new ArgumentOutOfRangeException(nameof(page),"Page must be positive and page size must be 1-100.");
  if(departmentId==Guid.Empty)throw new ArgumentException("Department filter cannot be empty.");
  if(status.HasValue&&!Enum.IsDefined(status.Value))throw new ArgumentOutOfRangeException(nameof(status));
  if(category.HasValue&&!Enum.IsDefined(category.Value))throw new ArgumentOutOfRangeException(nameof(category));
  var (items,count)=await repo.GetPageAsync(page,pageSize,departmentId,status,category,ct);
  return new(items.Select(Map).ToArray(),page,pageSize,count,count==0?0:(int)Math.Ceiling(count/(double)pageSize));
 }
 public async Task<EmployeeResponse> GetByIdAsync(Guid id,CancellationToken ct)=>Map(await Required(id,ct));
 public async Task<Guid> CreateAsync(CreateEmployeeRequest r,CancellationToken ct)
 {
  ArgumentNullException.ThrowIfNull(r);var number=NormalizeNumber(r.EmployeeNumber);
  if(!await repo.DepartmentIsActiveAsync(r.DepartmentId,ct))throw new KeyNotFoundException("Active department not found.");
  if(await repo.EmployeeNumberExistsAsync(number,null,ct))throw new InvalidOperationException("Employee number is already registered.");
  if(r.IdentityUserId.HasValue)
  {
   if(!await repo.IdentityExistsAsync(r.IdentityUserId.Value,ct))throw new KeyNotFoundException("Identity account not found.");
   if(await repo.IdentityAlreadyLinkedAsync(r.IdentityUserId.Value,ct))throw new InvalidOperationException("Identity account is already linked to an employee.");
  }
  var e=new Employee(number,r.FullName,r.Email,r.Designation,r.DepartmentId,r.Category,r.JoiningDate,r.PhoneNumber,r.IdentityUserId);
  await repo.AddAsync(e,ct);await unitOfWork.SaveChangesAsync(ct);return e.Id;
 }
 public async Task UpdateProfileAsync(Guid id,UpdateEmployeeProfileRequest r,CancellationToken ct){ArgumentNullException.ThrowIfNull(r);var e=await Required(id,ct);e.UpdateProfile(r.FullName,r.Email,r.Designation,r.PhoneNumber);await unitOfWork.SaveChangesAsync(ct);}
 public async Task TransferAsync(Guid id,TransferEmployeeRequest r,CancellationToken ct)
 {ArgumentNullException.ThrowIfNull(r);if(!await repo.DepartmentIsActiveAsync(r.DepartmentId,ct))throw new KeyNotFoundException("Active department not found.");var e=await Required(id,ct);e.TransferToDepartment(r.DepartmentId);await unitOfWork.SaveChangesAsync(ct);}
 public async Task ChangeStatusAsync(Guid id,ChangeEmployeeStatusRequest r,CancellationToken ct){ArgumentNullException.ThrowIfNull(r);var e=await Required(id,ct);e.ChangeStatus(r.Status,r.EffectiveDate);await unitOfWork.SaveChangesAsync(ct);}
 public async Task<TeacherProfileResponse> CreateTeacherProfileAsync(Guid id,CreateTeacherProfileRequest r,CancellationToken ct)
 {
  ArgumentNullException.ThrowIfNull(r);var e=await Required(id,ct);if(e.Category!=EmployeeCategory.Academic)throw new InvalidOperationException("Teacher profile requires an academic employee.");
  if(await repo.GetTeacherProfileAsync(id,ct) is not null)throw new InvalidOperationException("Teacher profile already exists.");
  var p=new TeacherProfile(id,r.HighestQualification,r.Specialization,r.AcademicRank);await repo.AddTeacherProfileAsync(p,ct);await unitOfWork.SaveChangesAsync(ct);return Map(p);
 }
 public async Task<TeacherProfileResponse> UpdateTeacherProfileAsync(Guid id,UpdateTeacherProfileRequest r,CancellationToken ct)
 {ArgumentNullException.ThrowIfNull(r);var p=await repo.GetTeacherProfileAsync(id,ct)??throw new KeyNotFoundException("Teacher profile not found.");p.Update(r.HighestQualification,r.Specialization,r.AcademicRank);await unitOfWork.SaveChangesAsync(ct);return Map(p);}
 private async Task<Employee> Required(Guid id,CancellationToken ct)=>id==Guid.Empty?throw new ArgumentException("Valid employee ID required."):await repo.GetByIdAsync(id,ct)??throw new KeyNotFoundException("Employee not found.");
 private static string NormalizeNumber(string n)=>string.IsNullOrWhiteSpace(n)?throw new ArgumentException("Employee number required."):n.Trim().ToUpperInvariant();
 private static EmployeeResponse Map(Employee e)=>new(e.Id,e.EmployeeNumber,e.FullName,e.Email,e.Designation,e.PhoneNumber,e.DepartmentId,e.Category,e.JoiningDate,e.Status,e.IdentityUserId,e.TerminationDate,e.CreatedAtUtc,e.UpdatedAtUtc);
 private static TeacherProfileResponse Map(TeacherProfile p)=>new(p.Id,p.EmployeeId,p.HighestQualification,p.Specialization,p.AcademicRank);
}