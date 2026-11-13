using UAF.Domain.Entities;
using UAF.Domain.Enums;
namespace UAF.Application.Features.Employees.Abstractions;
public interface IEmployeeRepository
{
 Task<bool> DepartmentIsActiveAsync(Guid id,CancellationToken ct);
 Task<bool> EmployeeNumberExistsAsync(string number,Guid? excludingId,CancellationToken ct);
 Task<bool> IdentityExistsAsync(Guid identityId,CancellationToken ct);
 Task<bool> IdentityAlreadyLinkedAsync(Guid identityId,CancellationToken ct);
 Task<(IReadOnlyList<Employee> Items,int TotalCount)> GetPageAsync(int page,int pageSize,Guid? departmentId,EmployeeStatus? status,EmployeeCategory? category,CancellationToken ct);
 Task<Employee?> GetByIdAsync(Guid id,CancellationToken ct);
 Task AddAsync(Employee employee,CancellationToken ct);
 Task<TeacherProfile?> GetTeacherProfileAsync(Guid employeeId,CancellationToken ct);
 Task AddTeacherProfileAsync(TeacherProfile profile,CancellationToken ct);
}