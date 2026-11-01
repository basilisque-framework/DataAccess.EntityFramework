extern alias SQLiteMigrations;
extern alias SqlServerMigrations;
extern alias PostgreSQLMigrations;

using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.Demo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Demo.Tests;

/// <summary>Regression coverage using the generated host registration without any selection workaround.</summary>
public sealed class MigrationAssemblyCollisionTests
{
    [Test]
    [Arguments("SQLite")]
    [Arguments("SqlServer")]
    [Arguments("PostgreSQL")]
    public async Task GeneratedRegistration_ShouldSelectMatchingMigrationAssembly_ForConfiguredProvider(string provider)
    {
        // Arrange
        await using var services = createServices(provider).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
        // Act
        var selected = context.GetService<IMigrationsAssembly>().Assembly.GetName().Name;
        // Assert
        await Assert.That(selected).IsEqualTo($"Basilisque.Demo.{provider}");
        await Assert.That(context.Database.HasPendingModelChanges()).IsFalse();
    }

    [Test]
    public async Task Migrate_ShouldApplySqliteMigrations_WithAllProviderAssembliesRegistered()
    {
        // Arrange
        await using var services = createServices("SQLite").BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
        await context.Database.OpenConnectionAsync();
        // Act
        await context.Database.MigrateAsync();
        // Assert
        await Assert.That((await context.Database.GetAppliedMigrationsAsync()).Count()).IsEqualTo(1);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task GeneratedMapping_ShouldIgnoreUnrelatedContext_WhenAssemblyOwnsOnlyDemoMigrations()
    {
        // Arrange
        await using var services = createServices("SQLite").BuildServiceProvider();
        var resolver = services.GetRequiredService<IMigrationAssemblyResolver>();
        // Act
        var selected = resolver.GetMigrationAssemblyName(typeof(UnrelatedContext), "SQLite");
        // Assert
        await Assert.That(selected).IsNull();
    }

    [Test]
    [Arguments("SQLite")]
    [Arguments("SqlServer")]
    [Arguments("PostgreSQL")]
    public async Task CreateDbContext_ShouldSelectOwnMigrationAssembly_InGeneratedDesignTimeFactory(string provider)
    {
        // Arrange: the generated factories use the same resolver, independently of the host.
        Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<DemoDbContext> factory = provider switch
        {
            "SQLite" => new SQLiteDemoFactory(),
            "SqlServer" => new SqlServerDemoFactory(),
            _ => new PostgreSQLDemoFactory()
        };
        // Act
        await using var context = factory.CreateDbContext([]);
        // Assert
        await Assert.That(context.GetService<IMigrationsAssembly>().Assembly.GetName().Name).IsEqualTo($"Basilisque.Demo.{provider}");
    }

    private static ServiceCollection createServices(string provider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(createConfiguration(provider));
        Basilisque.Demo.Web.IServiceCollectionExtensions.RegisterServices(services);
        return services;
    }

    private static IConfiguration createConfiguration(string provider) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["Database:ConnectionStrings:SQLite"] = "Data Source=:memory:",
            ["Database:ConnectionStrings:SqlServer"] = "Server=localhost;Database=Unused;Integrated Security=true;TrustServerCertificate=true",
            ["Database:ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=unused;Username=postgres"
        }).Build();

    private sealed class UnrelatedContext : DbContext;

    // Fully qualified generated types have identical names in the three provider assemblies.
    private sealed class SQLiteDemoFactory : SQLiteMigrations::Basilisque.Demo.Data.DemoDbContextDesignTimeFactory
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider services, string[] args) => createConfiguration("SQLite");
    }
    private sealed class SqlServerDemoFactory : SqlServerMigrations::Basilisque.Demo.Data.DemoDbContextDesignTimeFactory
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider services, string[] args) => createConfiguration("SqlServer");
    }
    private sealed class PostgreSQLDemoFactory : PostgreSQLMigrations::Basilisque.Demo.Data.DemoDbContextDesignTimeFactory
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider services, string[] args) => createConfiguration("PostgreSQL");
    }
}
