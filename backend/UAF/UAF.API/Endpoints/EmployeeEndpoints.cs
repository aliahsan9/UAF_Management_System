using UAF.API.Authorization;
using UAF.Application.Features.Employees;
using UAF.Application.Features.Employees.Models;
using UAF.Domain.Enums;
namespace UAF.API.Endpoints;
public static class EmployeeEndpoints
{
 public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
 {
  var g=endpoints.MapGroup("/api/v1/employees").WithTags("Employees").RequireAuthorization(AuthorizationPolicies.EmployeeManagement);
  g.MapGet("/",async(int page,int pageSize,Guid? departmentId,EmployeeStatus? status,EmployeeCategory? category,EmployeeService s,CancellationToken ct)=>Results.Ok(await s.GetPageAsync(page,pageSize,departmentId,status,category,ct)));
  g.MapGet("/{id:guid}",async(Guid id,EmployeeService s,CancellationToken ct)=>Results.Ok(await s.GetByIdAsync(id,ct)));
  g.MapPost("/",async(CreateEmployeeRequest r,EmployeeService s,CancellationToken ct)=>{var id=await s.CreateAsync(r,ct);return Results.Created($"/api/v1/employees/{id}",new{id});});
  g.MapPut("/{id:guid}/profile",async(Guid id,UpdateEmployeeProfileRequest r,EmployeeService s,CancellationToken ct)=>{await s.UpdateProfileAsync(id,r,ct);return Results.NoContent();});
  g.MapPut("/{id:guid}/department",async(Guid id,TransferEmployeeRequest r,EmployeeService s,CancellationToken ct)=>{await s.TransferAsync(id,r,ct);return Results.NoContent();});
  g.MapPut("/{id:guid}/status",async(Guid id,ChangeEmployeeStatusRequest r,EmployeeService s,CancellationToken ct)=>{await s.ChangeStatusAsync(id,r,ct);return Results.NoContent();});
  g.MapPost("/{id:guid}/teacher-profile",async(Guid id,CreateTeacherProfileRequest r,EmployeeService s,CancellationToken ct)=>Results.Created($"/api/v1/employees/{id}/teacher-profile",await s.CreateTeacherProfileAsync(id,r,ct)));
  g.MapPut("/{id:guid}/teacher-profile",async(Guid id,UpdateTeacherProfileRequest r,EmployeeService s,CancellationToken ct)=>Results.Ok(await s.UpdateTeacherProfileAsync(id,r,ct)));
  return endpoints;
 }
}