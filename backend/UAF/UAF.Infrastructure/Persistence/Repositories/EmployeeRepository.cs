using Microsoft.EntityFrameworkCore;
using UAF.Application.Features.Employees.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Infrastructure.Persistence.Repositories;
internal sealed class EmployeeRepository:IEmployeeRepository
{
 private readonly UafDbContext db;
 public EmployeeRepository(UafDbContext db)=>this.db=db;
 public Task<bool> DepartmentIsActiveAsync(Guid id,CancellationToken ct)=>db.Departments.AnyAsync(x=>x.Id==id&&x.IsActive,ct);
 public Task<bool> EmployeeNumberExistsAsync(string n,Guid? excludingId,CancellationToken ct)=>db.Employees.AnyAsync(x=>x.EmployeeNumber==n&&(!excludingId.HasValue||x.Id!=excludingId.Value),ct);
 public Task<bool> IdentityExistsAsync(Guid id,CancellationToken ct)=>db.Users.AnyAsync(x=>x.Id==id&&x.IsActive,ct);
 public Task<bool> IdentityAlreadyLinkedAsync(Guid id,CancellationToken ct)=>db.Employees.AnyAsync(x=>x.IdentityUserId==id,ct);
 public async Task<(IReadOnlyList<Employee> Items,int TotalCount)> GetPageAsync(int page,int size,Guid? departmentId,EmployeeStatus? status,EmployeeCategory? category,CancellationToken ct)
 {
  var q=db.Employees.AsNoTracking();
  if(departmentId.HasValue)q=q.Where(x=>x.DepartmentId==departmentId.Value);
  if(status.HasValue)q=q.Where(x=>x.Status==status.Value);
  if(category.HasValue)q=q.Where(x=>x.Category==category.Value);
  var count=await q.CountAsync(ct);var items=await q.OrderBy(x=>x.EmployeeNumber).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync(ct);
  return(items,count);
 }
 public Task<Employee?> GetByIdAsync(Guid id,CancellationToken ct)=>db.Employees.FirstOrDefaultAsync(x=>x.Id==id,ct);
 public Task AddAsync(Employee e,CancellationToken ct)=>db.Employees.AddAsync(e,ct).AsTask();
 public Task<TeacherProfile?> GetTeacherProfileAsync(Guid id,CancellationToken ct)=>db.TeacherProfiles.FirstOrDefaultAsync(x=>x.EmployeeId==id,ct);
 public Task AddTeacherProfileAsync(TeacherProfile p,CancellationToken ct)=>db.TeacherProfiles.AddAsync(p,ct).AsTask();
}