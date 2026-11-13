using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UAF.Application.Features.Fees.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using UAF.Infrastructure;
using UAF.Infrastructure.Identity;
using UAF.Infrastructure.Persistence;
using Xunit;
namespace UAF.Tests.Integration;
[Trait("Category","SqlServerIntegration")]
public sealed class FeeSqlServerIntegrationTests
{
 [SqlServerFact]
 public async Task ConcurrentPaymentVerificationCannotOverpayChallan()
 {
  var configured=Environment.GetEnvironmentVariable("UAF_TEST_SQLSERVER_CONNECTION_STRING")!;
  var name=$"UafFeeTests_{Guid.NewGuid():N}";
  var master=new SqlConnectionStringBuilder(configured){InitialCatalog="master"};
  var target=new SqlConnectionStringBuilder(configured){InitialCatalog=name};
  await using(var c=new SqlConnection(master.ConnectionString)){await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText=$"CREATE DATABASE [{name}]";await cmd.ExecuteNonQueryAsync();}
  try
  {
   var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:UafDatabase",target.ConnectionString}}).Build();
   var services=new ServiceCollection();services.AddLogging();services.AddInfrastructure(cfg);using var provider=services.BuildServiceProvider();
   var studentId=Guid.NewGuid();var recorderId=Guid.NewGuid();var verifierId=Guid.NewGuid();var programId=Guid.NewGuid();var sessionId=Guid.NewGuid();var headId=Guid.NewGuid();var structureId=Guid.NewGuid();
   using(var scope=provider.CreateScope())
   {
    var db=scope.ServiceProvider.GetRequiredService<UafDbContext>();await db.Database.EnsureCreatedAsync();
    var uni=new University("Test University","TU");var faculty=new Faculty(uni.Id,"Faculty","F");var dept=new Department(faculty.Id,"Department","D");
    var program=new AcademicProgram(dept.Id,"Computer Science","CS",DegreeLevel.Undergraduate,4);programId=program.Id;
    var session=new AcademicSession("Fall 2026","FALL-2026",new DateOnly(2026,8,1),new DateOnly(2026,12,31));sessionId=session.Id;
    var student=new Student("UAF-TEST-001","Test Student","student@test.example",program.Id,new DateOnly(2026,9,1));studentId=student.Id;
    var head=new FeeHead("TUITION","Tuition Fee");headId=head.Id;
    var structure=new FeeStructure(program.Id,session.Id,1,"Fall Semester Fees");structureId=structure.Id;
    db.AddRange(uni,faculty,dept,program,session,student,head,structure,
      new FeeStructureItem(structure.Id,head.Id,1000m),
      CreateUser(recorderId,"recorder"),CreateUser(verifierId,"verifier"));
    await db.SaveChangesAsync();
   }
   Guid challanId;
   using(var scope=provider.CreateScope())
   {
    var repo=scope.ServiceProvider.GetRequiredService<IFeeRepository>();
    await repo.PublishStructureAsync(structureId,CancellationToken.None);
    challanId=await repo.GenerateChallanAsync(studentId,structureId,new DateOnly(2026,11,13),new DateOnly(2026,11,30),0m,recorderId,CancellationToken.None);
    await repo.RecordPaymentAsync(challanId,600m,"Bank","REF-A",recorderId,CancellationToken.None);
    await repo.RecordPaymentAsync(challanId,600m,"Bank","REF-B",recorderId,CancellationToken.None);
   }
   async Task<bool> Verify(string reference)
   {
    using var scope=provider.CreateScope();var db=scope.ServiceProvider.GetRequiredService<UafDbContext>();var repo=scope.ServiceProvider.GetRequiredService<IFeeRepository>();
    var id=await db.FeePayments.Where(x=>x.ReferenceNumber==reference).Select(x=>x.Id).SingleAsync();
    try{await repo.VerifyPaymentAsync(id,verifierId,true,"Bank statement matched",CancellationToken.None);return true;}
    catch(InvalidOperationException){return false;}
   }
   var results=await Task.WhenAll(Verify("REF-A"),Verify("REF-B"));
   Assert.Single(results.Where(x=>x));Assert.Single(results.Where(x=>!x));
   using(var scope=provider.CreateScope())
   {
    var db=scope.ServiceProvider.GetRequiredService<UafDbContext>();
    Assert.Equal(1,await db.FeePayments.CountAsync(x=>x.Status==FeePaymentStatus.Verified));
    Assert.Equal(1,await db.FeePayments.CountAsync(x=>x.Status==FeePaymentStatus.PendingVerification));
    var repo=scope.ServiceProvider.GetRequiredService<IFeeRepository>();
    Assert.Equal(400m,await repo.GetOutstandingBalanceAsync(studentId,CancellationToken.None));
   }
  }
  finally
  {
   await using var c=new SqlConnection(master.ConnectionString);await c.OpenAsync();await using var cmd=c.CreateCommand();
   cmd.CommandText=$"IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
   await cmd.ExecuteNonQueryAsync();
  }
 }
 private static ApplicationUser CreateUser(Guid id,string name)=>new(){Id=id,UserName=name,NormalizedUserName=name.ToUpperInvariant(),Email=$"{name}@example.com",NormalizedEmail=$"{name}@example.com".ToUpperInvariant(),EmailConfirmed=true,SecurityStamp=Guid.NewGuid().ToString("N"),ConcurrencyStamp=Guid.NewGuid().ToString("N"),FullName=name,IsActive=true};
}