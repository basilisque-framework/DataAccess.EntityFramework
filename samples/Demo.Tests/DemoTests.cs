using Basilisque.Demo.Data;
using Basilisque.Demo.Web.Services;
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Basilisque.Demo.Tests;

public sealed class DemoTests
{
    [Test]
    [Arguments("SQLite", "Microsoft.EntityFrameworkCore.Sqlite", "Basilisque.Demo.SQLite")]
    [Arguments("SqlServer", "Microsoft.EntityFrameworkCore.SqlServer", "Basilisque.Demo.SqlServer")]
    [Arguments("PostgreSQL", "Npgsql.EntityFrameworkCore.PostgreSQL", "Basilisque.Demo.PostgreSQL")]
    public async Task EachProviderResolvesItsModelAndMigrations(string provider, string expectedProvider, string expectedAssembly)
    {
        await using var services = createServices(provider, "Data Source=unused.db");
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoDbContext>();

        await Assert.That(context.Database.ProviderName).IsEqualTo(expectedProvider);
        await Assert.That(context.GetService<IMigrationsAssembly>().Assembly.GetName().Name).IsEqualTo(expectedAssembly);
        await Assert.That(context.Database.GetMigrations().Count()).IsEqualTo(1);
        await Assert.That(context.Database.HasPendingModelChanges()).IsFalse();
        var script = context.GetService<IMigrator>().GenerateScript();
        await Assert.That(script.Contains("Demo_Documents")).IsTrue();
        await Assert.That(script.Contains("Demo_DocumentsDeleteAudit")).IsTrue();
    }

    [Test]
    public async Task GeneratedRegistrationIncludesTheInitializerAndSeedingIsIdempotent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"BasilisqueDemo-{Guid.NewGuid():N}.db");
        try
        {
            await using var services = createServices("SQLite", $"Data Source={path};Pooling=False");
            await using var scope = services.CreateAsyncScope();
            var initializers = scope.ServiceProvider.GetServices<IDbInitializer>().ToList();
            await Assert.That(initializers.Count).IsEqualTo(1);
            var initializer = initializers.Single();
            initializer.BeforeMigration();
            initializer.MigrateDatabase();
            initializer.AfterMigration();
            initializer.SeedData();
            initializer.SeedData();
            var snapshot = await scope.ServiceProvider.GetRequiredService<DemoSession>()
                .RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.All));
            await Assert.That(snapshot.Documents.Count).IsEqualTo(1);
            await Assert.That(snapshot.Documents[0].CreatedBy).IsEqualTo(DemoUsers.Alice);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task DocumentLifecyclePreservesStampsAndRecordsOnlyPhysicalDeletion()
    {
        await withSqliteAsync(async session =>
        {
            var created = await session.RunAsync(DemoUsers.Alice, scenarios => scenarios.CreateAsync("First", "Original"));
            var first = created.After!;
            await Assert.That(created.Before).IsNull();
            await Assert.That(first.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(first.CreatedBy).IsEqualTo(DemoUsers.Alice);
            await Assert.That(first.ModifiedBy).IsEqualTo(DemoUsers.Alice);
            await Assert.That(first.CreatedAt).IsEqualTo(first.ModifiedAt);

            var updated = await session.RunAsync(DemoUsers.Bob,
                scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.Update, "Edited", "Updated"));
            await Assert.That(updated.Before!.Title).IsEqualTo("First");
            await Assert.That(updated.After!.ModifiedBy).IsEqualTo(DemoUsers.Bob);
            await Assert.That(updated.After.CreatedBy).IsEqualTo(DemoUsers.Alice);
            await Assert.That(updated.After.CreatedAt).IsEqualTo(first.CreatedAt);
            await Assert.That(updated.After.ModifiedAt >= first.ModifiedAt).IsTrue();

            var deleted = await session.RunAsync(DemoUsers.Bob,
                scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.SoftDelete, "", ""));
            await Assert.That(deleted.After!.DeletedBy).IsEqualTo(DemoUsers.Bob);
            await Assert.That(deleted.After.DeletedAt).IsNotNull();
            await Assert.That(deleted.After.Title).IsEqualTo("Edited");
            await Assert.That(deleted.After.CreatedAt).IsEqualTo(first.CreatedAt);
            var active = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.Active));
            var hidden = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.Deleted));
            await Assert.That(active.Documents.Count).IsEqualTo(0);
            await Assert.That(hidden.Documents.Count).IsEqualTo(1);
            await Assert.That(hidden.Deletions.Count).IsEqualTo(0);

            var restored = await session.RunAsync(DemoUsers.Alice,
                scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.Restore, "", ""));
            await Assert.That(restored.After!.DeletedAt).IsNull();
            await Assert.That(restored.After.DeletedBy).IsNull();
            await Assert.That(restored.After.ModifiedBy).IsEqualTo(DemoUsers.Alice);

            var removed = await session.RunAsync(DemoUsers.Bob,
                scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.HardDelete, "", ""));
            await Assert.That(removed.After).IsNull();
            var final = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.All));
            await Assert.That(final.Documents.Count).IsEqualTo(0);
            await Assert.That(final.Deletions.Count).IsEqualTo(1);
            await Assert.That(final.Deletions[0].EntityKey).IsEqualTo(first.Id);
            await Assert.That(final.Deletions[0].DeletedBy).IsEqualTo(DemoUsers.Bob);
        });
    }

    [Test]
    public async Task FreshOperationScopesDoNotLeakUsers()
    {
        await withSqliteAsync(async session =>
        {
            var first = (await session.RunAsync(DemoUsers.Alice, scenarios => scenarios.CreateAsync("Alice", ""))).After!;
            var updated = (await session.RunAsync(null,
                scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.Update, "No actor", ""))).After!;
            await Assert.That(updated.ModifiedBy).IsEqualTo(DemoUsers.Alice);
            await Assert.That(updated.ModifiedAt >= first.ModifiedAt).IsTrue();

            var anonymous = (await session.RunAsync(null, scenarios => scenarios.CreateAsync("Anonymous", ""))).After!;
            await Assert.That(anonymous.CreatedBy).IsEqualTo(Guid.Empty);
            await Assert.That(anonymous.ModifiedBy).IsEqualTo(Guid.Empty);
            await Assert.That(anonymous.CreatedAt > DateTimeOffset.MinValue).IsTrue();
            await session.RunAsync(null, scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.HardDelete, "", ""));
            var snapshot = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.All));
            await Assert.That(snapshot.Deletions.Single().DeletedBy).IsNull();
        });
    }

    [Test]
    public async Task ResetClearsDemoRowsAndAuditWithoutDroppingMigrations()
    {
        await withSqliteAsync(async session =>
        {
            var first = (await session.RunAsync(DemoUsers.Alice, scenarios => scenarios.CreateAsync("Delete", ""))).After!;
            await session.RunAsync(DemoUsers.Bob, scenarios => scenarios.ExecuteAsync(first.Id, DocumentAction.HardDelete, "", ""));
            await session.RunAsync(DemoUsers.Alice, scenarios => scenarios.CreateAsync("Keep until reset", ""));
            await session.RunAsync(null, scenarios => scenarios.ResetAsync());
            var snapshot = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.All));
            await Assert.That(snapshot.Documents.Count).IsEqualTo(0);
            await Assert.That(snapshot.Deletions.Count).IsEqualTo(0);
            var next = await session.RunAsync(DemoUsers.Bob, scenarios => scenarios.CreateAsync("After reset", ""));
            await Assert.That(next.After!.CreatedBy).IsEqualTo(DemoUsers.Bob);
        });
    }

    [Test]
    public async Task InvalidInputDoesNotPersistData()
    {
        await withSqliteAsync(async session =>
        {
            await Assert.That(async () => await session.RunAsync(DemoUsers.Alice,
                scenarios => scenarios.CreateAsync(" ", ""))).Throws<ArgumentException>();
            await Assert.That(async () => await session.RunAsync(DemoUsers.Alice,
                scenarios => scenarios.CreateAsync("Title", new string('x', 4001)))).Throws<ArgumentException>();
            var snapshot = await session.RunAsync(null, scenarios => scenarios.ReadAsync(DocumentView.All));
            await Assert.That(snapshot.Documents.Count).IsEqualTo(0);
        });
    }

    private static ServiceProvider createServices(string provider, string sqliteConnection)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["Database:ConnectionStrings:SQLite"] = sqliteConnection,
            ["Database:ConnectionStrings:SqlServer"] = "Server=localhost;Database=BasilisqueEfDemo;Integrated Security=true;TrustServerCertificate=true",
            ["Database:ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=basilisque_ef_demo;Username=postgres"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDemo(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static async Task withSqliteAsync(Func<DemoSession, Task> action)
    {
        var path = Path.Combine(Path.GetTempPath(), $"BasilisqueDemo-{Guid.NewGuid():N}.db");
        try
        {
            await using var services = createServices("SQLite", $"Data Source={path};Pooling=False");
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DemoDbContext>().Database.MigrateAsync();
            await action(scope.ServiceProvider.GetRequiredService<DemoSession>());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
