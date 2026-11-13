using System.Reflection;
using UAF.Application;
using UAF.Domain.Common;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Architecture;

/// <summary>
/// Protects the compile-time boundaries of the modular monolith.
/// Feature-level rules are added as modules are extracted and stabilized.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(UafDbContext).Assembly;
    private static readonly Assembly Api = typeof(global::Program).Assembly;

    [Fact]
    public void Domain_MustNotDependOnOuterLayers()
    {
        AssertDoesNotReference(Domain, "UAF.Application", "UAF.Infrastructure", "UAF.API");
    }

    [Fact]
    public void Application_MustNotDependOnInfrastructureOrApi()
    {
        AssertDoesNotReference(Application, "UAF.Infrastructure", "UAF.API");
    }

    [Fact]
    public void Infrastructure_MustNotDependOnApi()
    {
        AssertDoesNotReference(Infrastructure, "UAF.API");
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenAssemblies)
    {
        var references = assembly.GetReferencedAssemblies().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var forbidden in forbiddenAssemblies)
        {
            Assert.DoesNotContain(forbidden, references);
        }
    }
}
