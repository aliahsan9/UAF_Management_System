using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using UAF.API.Authorization;
using Xunit;
namespace UAF.Tests.Authorization;
public sealed class FinanceRolePolicyTests
{
 [Theory]
 [InlineData("FinanceOfficer",true)]
 [InlineData("SuperAdmin",true)]
 [InlineData("Registrar",false)]
 [InlineData("DepartmentAdmin",false)]
 [InlineData("Student",false)]
 public async Task OnlyFinanceOfficerAndSuperAdminCanAccessFinanceEndpoints(string role,bool expected)
 {
  var services=new ServiceCollection();services.AddLogging();services.AddUafAuthorization();using var provider=services.BuildServiceProvider();
  var principal=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()),new Claim(ClaimTypes.Role,role)},"test"));
  var result=await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal,null,AuthorizationPolicies.FinanceManagement);
  Assert.Equal(expected,result.Succeeded);
 }
}