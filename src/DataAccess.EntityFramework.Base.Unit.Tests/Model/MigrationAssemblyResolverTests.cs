using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Model;

/// <summary>Tests selection independently of DI registration order and unrelated contexts.</summary>
public sealed class MigrationAssemblyResolverTests
{
    private readonly IMigrationAssemblyProvider _first = Substitute.For<IMigrationAssemblyProvider>();
    private readonly IMigrationAssemblyProvider _second = Substitute.For<IMigrationAssemblyProvider>();

    /// <summary>Creates isolated mappings for two contexts and two database providers.</summary>
    public MigrationAssemblyResolverTests()
    {
        _first.GetMigrationAssemblyName(typeof(FirstContext), "SQLite").Returns("First.SQLite");
        _second.GetMigrationAssemblyName(typeof(SecondContext), "SQLite").Returns("Second.SQLite");
        _second.GetMigrationAssemblyName(typeof(FirstContext), "SqlServer").Returns("First.SqlServer");
    }

    [Test]
    [Arguments(false, "SQLite", "First.SQLite")]
    [Arguments(true, "SQLite", "First.SQLite")]
    [Arguments(false, "SqlServer", "First.SqlServer")]
    [Arguments(true, "SqlServer", "First.SqlServer")]
    public async Task GetMigrationAssemblyName_ShouldSelectMatchingPair_InEitherRegistrationOrder(bool reverse, string provider, string expected)
    {
        // Arrange
        var resolver = new MigrationAssemblyResolver(reverse ? [_second, _first] : [_first, _second], []);
        // Act
        var result = resolver.GetMigrationAssemblyName(typeof(FirstContext), provider);
        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GetMigrationAssemblyName_ShouldUseOtherContextMapping_InSameHost()
    {
        // Arrange
        var resolver = new MigrationAssemblyResolver([_first, _second], []);
        // Act
        var result = resolver.GetMigrationAssemblyName(typeof(SecondContext), "SQLite");
        // Assert
        await Assert.That(result).IsEqualTo("Second.SQLite");
    }

    [Test]
    public async Task GetMigrationAssemblyName_ShouldReturnNull_WhenNoMappingMatches()
    {
        // Arrange
        var resolver = new MigrationAssemblyResolver([_first, _second], []);
        // Act
        var result = resolver.GetMigrationAssemblyName(typeof(SecondContext), "PostgreSQL");
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetMigrationAssemblyName_ShouldAcceptIdenticalMappings_InRepeatedRegistration()
    {
        // Arrange
        var resolver = new MigrationAssemblyResolver([_first, _first], []);
        // Act
        var result = resolver.GetMigrationAssemblyName(typeof(FirstContext), "SQLite");
        // Assert
        await Assert.That(result).IsEqualTo("First.SQLite");
    }

    [Test]
    public async Task GetMigrationAssemblyName_ShouldExplainAmbiguity_WhenDifferentAssembliesMatch()
    {
        // Arrange
        _second.GetMigrationAssemblyName(typeof(FirstContext), "SQLite").Returns("Conflicting.SQLite");
        var resolver = new MigrationAssemblyResolver([_first, _second], []);
        // Act
        var error = await Assert.That(() => resolver.GetMigrationAssemblyName(typeof(FirstContext), "SQLite")).Throws<InvalidOperationException>();
        // Assert
        await Assert.That(error!.Message.Contains(typeof(FirstContext).FullName!, StringComparison.Ordinal)).IsTrue();
        await Assert.That(error.Message.Contains("First.SQLite", StringComparison.Ordinal)).IsTrue();
        await Assert.That(error.Message.Contains("Conflicting.SQLite", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task AddMigrationAssembly_ShouldOverrideOnlyMatchingPair_WithGeneratedMappingsPresent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_first);
        services.AddSingleton(_second);
        services.AddMigrationAssembly<FirstContext>("SQLite", typeof(FirstContext).Assembly);
        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IMigrationAssemblyResolver>();
        // Act
        var result = resolver.GetMigrationAssemblyName(typeof(FirstContext), "SQLite");
        // Assert
        await Assert.That(result).IsEqualTo(typeof(FirstContext).Assembly.GetName().Name);
        await Assert.That(resolver.GetMigrationAssemblyName(typeof(SecondContext), "SQLite")).IsEqualTo("Second.SQLite");
    }

    [Test]
    public async Task GetMigrationAssemblyName_ShouldRejectConflictingOverrides_ForSamePair()
    {
        // Arrange
        var resolver = new MigrationAssemblyResolver([_first], [
            new(typeof(FirstContext), "SQLite", "Override.One"),
            new(typeof(FirstContext), "SQLite", "Override.Two")]);
        // Act / Assert
        await Assert.That(() => resolver.GetMigrationAssemblyName(typeof(FirstContext), "SQLite")).Throws<InvalidOperationException>();
    }

    private sealed class FirstContext : DbContext;
    private sealed class SecondContext : DbContext;
}
