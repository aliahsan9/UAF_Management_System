using System.Security.Claims;
using UAF.API.Authorization;
using UAF.Application.Features.Fees;
using UAF.Application.Features.Fees.Models;
namespace UAF.API.Endpoints;
public static class FeeEndpoints
{
 public static IEndpointRouteBuilder MapFeeEndpoints(this IEndpointRouteBuilder endpoints)
 {
  var g=endpoints.MapGroup("/api/v1/finance").WithTags("Finance").RequireAuthorization(AuthorizationPolicies.FinanceManagement);
  g.MapPost("/fee-heads",async(CreateFeeHeadRequest r,FeeService s,CancellationToken ct)=>Results.Created($"/api/v1/finance/fee-heads",(object)new{id=await s.CreateFeeHeadAsync(r,ct)}));
  g.MapPost("/fee-structures",async(CreateFeeStructureRequest r,FeeService s,CancellationToken ct)=>Results.Created($"/api/v1/finance/fee-structures",(object)new{id=await s.CreateStructureAsync(r,ct)}));
  g.MapPost("/fee-structures/{id:guid}/publish",async(Guid id,FeeService s,CancellationToken ct)=>{await s.PublishStructureAsync(id,ct);return Results.NoContent();});
  g.MapPost("/challans",async(GenerateFeeChallanRequest r,ClaimsPrincipal p,FeeService s,CancellationToken ct)=>Results.Created($"/api/v1/finance/challans",(object)new{id=await s.GenerateChallanAsync(r,Actor(p),ct)}));
  g.MapPost("/challans/{id:guid}/payments",async(Guid id,RecordFeePaymentRequest r,ClaimsPrincipal p,FeeService s,CancellationToken ct)=>Results.Created($"/api/v1/finance/payments",(object)new{id=await s.RecordPaymentAsync(id,r,Actor(p),ct)}));
  g.MapPost("/payments/{id:guid}/verification",async(Guid id,VerifyFeePaymentRequest r,ClaimsPrincipal p,FeeService s,CancellationToken ct)=>{await s.VerifyPaymentAsync(id,r,Actor(p),ct);return Results.NoContent();});
  g.MapGet("/students/{studentId:guid}/account",async(Guid studentId,FeeService s,CancellationToken ct)=>Results.Ok(await s.GetAccountAsync(studentId,ct)));
  return endpoints;
 }
 private static Guid Actor(ClaimsPrincipal p)
 {
  var value=p.FindFirstValue(ClaimTypes.NameIdentifier)??p.FindFirstValue("sub");
  return Guid.TryParse(value,out var id)&&id!=Guid.Empty?id:throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
 }
}