using System.Reflection;using UAF.Domain.Common;using UAF.Application;using UAF.Infrastructure.Persistence;using Xunit;
namespace UAF.Tests.Architecture;
public sealed class ArchitectureBoundaryTests{
 [Fact] public void Domain_HasNoDependenciesOnOuterLayers()=>AssertForbidden(typeof(Entity).Assembly,"UAF.Application","UAF.Infrastructure","UAF.API");
 [Fact] public void Application_DoesNotDependOnInfrastructureOrApi()=>AssertForbidden(typeof(DependencyInjection).Assembly,"UAF.Infrastructure","UAF.API");
 [Fact] public void Infrastructure_DoesNotDependOnApi()=>AssertForbidden(typeof(UafDbContext).Assembly,"UAF.API");
 [Fact] public void AllProductionProjects_TargetNet10()=>Assert.All(new[]{typeof(Entity).Assembly,typeof(DependencyInjection).Assembly,typeof(UafDbContext).Assembly,typeof(global::Program).Assembly},a=>Assert.StartsWith(".NETCoreApp,Version=v10.0",a.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName??string.Empty));
 private static void AssertForbidden(Assembly assembly,params string[] forbidden){var refs=assembly.GetReferencedAssemblies().Select(x=>x.Name).ToHashSet(StringComparer.Ordinal);foreach(var name in forbidden)Assert.DoesNotContain(name,refs);}
}
