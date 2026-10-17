using Basilisque.Core.Auth;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Basilisque.DataAccess.EntityFramework.Relational.Design;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.Data.Sqlite;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace Basilisque.DataAccess.EntityFramework.Relational.Unit.Tests.HardDelete;

public class HardDeleteAuditTests
{
    private sealed class Document : IHardDeleteAudited
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public int Version { get; set; }
    }

    private sealed class OtherDocument : IHardDeleteAudited
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    private sealed class SoftDocument : ISoftDelete, IHardDeleteAudited
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
    }

    private sealed class StringActorDocument : IHardDeleteAudited<string>
    {
        public int Id { get; set; }
    }

    private sealed class IntActorDocument : IHardDeleteAudited<int>
    {
        public int Id { get; set; }
    }

    public readonly record struct DocumentId(Guid Value);

    private sealed class ConvertedDocument : IHardDeleteAudited
    {
        public DocumentId Id { get; set; }
    }

    private sealed class Parent : IHardDeleteAudited
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<Child> Children { get; set; } = [];
    }

    private sealed class Child : IHardDeleteAudited
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParentId { get; set; }
    }

    private sealed class CompositeDocument
    {
        public Guid TenantId { get; set; }
        public int Number { get; set; }
    }

    private sealed class DynamicDocument
    {
        public Guid TenantId { get; set; }
        public string Number { get; set; } = "";
    }

    public sealed class DocumentKey
    {
        public Guid TenantId { get; set; }
        public int Number { get; set; }
    }

    private sealed class PlainDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    private class Root
    {
        public Guid Id { get; set; }
    }

    private sealed class Derived : Root
    {
    }

    private sealed class MissingConventionContext(DbContextOptions<MissingConventionContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Document>();
    }

    private sealed class AutoContext(IDbProviderServiceProvider provider) :
        Basilisque.DataAccess.EntityFramework.Relational.Model.BaseDbContext<AutoContext>(provider)
    {
        protected override string? GetAppAreaPrefix() => "App";

        protected override void OnAfterModelCreating(ModelBuilder builder)
        {
            builder.Entity<Document>().ToTable("Documents");
        }
    }

    private sealed class Configurator(SqliteConnection connection) : IDbContextOptionsConfigurator
    {
        public void Configure<TDbContext>(BaseDbContext<TDbContext> context, DbContextOptionsBuilder builder)
            where TDbContext : BaseDbContext<TDbContext> => builder.UseSqlite(connection);
    }

    private sealed class DesignFactory(SqliteConnection connection) : RelationalBaseDesignTimeDbContextFactory<AutoContext>
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args) =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build();

        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddSingleton<IDbContextOptionsConfigurator>(new Configurator(connection));
            var info = Substitute.For<IDbProviderInfo>();
            info.ProviderKey.Returns("SQLite");
            services.AddSingleton(info);
        }
    }

    public sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.AddHardDeleteAuditConvention();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Document>().Property(document => document.Version).IsConcurrencyToken();
            builder.Entity<OtherDocument>();
            builder.Entity<SoftDocument>();
            builder.Entity<StringActorDocument>();
            builder.Entity<IntActorDocument>();
            builder.Entity<ConvertedDocument>().Property(value => value.Id)
                .HasConversion(value => value.Value, value => new DocumentId(value)).ValueGeneratedNever();
            builder.Entity<Parent>().HasMany(value => value.Children).WithOne().HasForeignKey(value => value.ParentId)
                .OnDelete(DeleteBehavior.ClientCascade);
            builder.Entity<PlainDocument>();
            builder.Entity<CompositeDocument>().HasKey(document => new { document.TenantId, document.Number });
            builder.Entity<CompositeDocument>().UseHardDeleteAudit(audit => audit.HasEntityKey<DocumentKey>(key =>
            {
                key.Property(value => value.TenantId, document => document.TenantId);
                key.Property(value => value.Number, document => document.Number);
            }));
            builder.Entity<DynamicDocument>().HasKey(document => new { document.TenantId, document.Number });
            builder.Entity<DynamicDocument>().Property(document => document.Number).HasMaxLength(40).IsUnicode(false);
            builder.Entity<DynamicDocument>().UseHardDeleteAudit(audit => audit.ToTable("DynamicDeletions"));
            builder.ApplySoftDeleteConfigurations();
        }
    }

    private sealed class Database : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly ServiceProvider _services;
        private readonly IServiceScope _scope;

        public Database()
        {
            _connection.Open();
            var services = new ServiceCollection();
            Basilisque.DataAccess.EntityFramework.Relational.IServiceCollectionExtensions.RegisterServices(services);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build());
            _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            _scope = _services.CreateScope();
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public void SetUser<T>(T user) where T : notnull =>
            _scope.ServiceProvider.GetRequiredService<IWritableUserContext<T>>().UserId = user;

        public TestContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestContext>().UseSqlite(_connection);
            options.UseHardDeleteAudit(_scope.ServiceProvider);
            return new TestContext(options.Options);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _services.Dispose();
            _connection.Dispose();
        }
    }

    [Test]
    public async Task Interface_enables_separate_typed_events_with_actor_and_no_foreign_keys()
    {
        using var database = new Database();
        var user = Guid.NewGuid();
        database.SetUser(user);
        using var context = database.CreateContext();
        var document = new Document();
        var other = new OtherDocument();
        context.AddRange(document, other);
        await context.SaveChangesAsync();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        var before = DateTimeOffset.UtcNow;
        context.RemoveRange(document, other);
        await context.SaveChangesAsync();
        var audit = await context.HardDeleteAudits<Document, Guid>().SingleAsync(value => value.EntityKey == document.Id);
        await Assert.That(audit.DeletedBy).IsEqualTo(user);
        await Assert.That(audit.AuditId == Guid.Empty).IsFalse();
        await Assert.That(audit.DeletedAt >= before && audit.DeletedAt <= DateTimeOffset.UtcNow).IsTrue();
        await Assert.That((await context.HardDeleteAudits<OtherDocument, Guid>().SingleAsync()).EntityKey).IsEqualTo(other.Id);
        await Assert.That(context.Model.GetEntityTypes().Where(type => type.GetTableName()!.EndsWith("DeleteAudit"))
            .All(type => !type.GetForeignKeys().Any())).IsTrue();
        context.ChangeTracker.Clear();
        await Assert.That(await context.Set<Document>().CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Sync_save_without_actor_records_null_and_does_not_repeat_events()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        context.SaveChanges();
        context.SaveChanges();
        var audit = context.HardDeleteAudits<Document, Guid>().Single();
        await Assert.That(audit.DeletedBy).IsNull();
        await Assert.That(context.ChangeTracker.Entries().Any(entry => entry.State == EntityState.Added)).IsFalse();
    }

    [Test]
    public async Task Soft_delete_and_restore_do_not_emit_physical_events_but_explicit_hard_delete_does()
    {
        using var database = new Database();
        var user = Guid.NewGuid();
        database.SetUser(user);
        using var context = database.CreateContext();
        var document = new SoftDocument();
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();
        await Assert.That(document.DeletedBy).IsEqualTo(user);
        await Assert.That(await context.HardDeleteAudits<SoftDocument, Guid>().CountAsync()).IsEqualTo(0);
        document.DeletedAt = null;
        await context.SaveChangesAsync();
        await Assert.That(document.DeletedBy).IsNull();
        using (SoftDeleteSuppressor.AllowHardDelete())
        {
            context.Remove(document);
            await context.SaveChangesAsync();
        }
        await Assert.That((await context.HardDeleteAudits<SoftDocument, Guid>().SingleAsync()).DeletedBy).IsEqualTo(user);
    }

    [Test]
    public async Task Typed_and_dynamic_composite_keys_are_queryable_in_SQL()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var tenant = Guid.NewGuid();
        var typed = new CompositeDocument { TenantId = tenant, Number = 12 };
        var dynamic = new DynamicDocument { TenantId = tenant, Number = "A-15" };
        context.AddRange(typed, dynamic);
        await context.SaveChangesAsync();
        context.RemoveRange(typed, dynamic);
        await context.SaveChangesAsync();
        var audit = await context.HardDeleteAudits<CompositeDocument, DocumentKey>()
            .SingleAsync(value => value.EntityKey.TenantId == tenant && value.EntityKey.Number == 12);
        await Assert.That(audit.EntityKey.Number).IsEqualTo(12);
        var dynamicAudit = await context.HardDeleteAudits<DynamicDocument>().SingleAsync(value =>
            EF.Property<Guid>(value, "EntityKey_TenantId") == tenant &&
            EF.Property<string>(value, "EntityKey_Number") == "A-15");
        await Assert.That((string)dynamicAudit["EntityKey_Number"]).IsEqualTo("A-15");
        var property = context.Model.GetEntityTypes().Single(type => type.GetTableName() == "DynamicDeletions")
            .FindProperty("EntityKey_Number")!;
        await Assert.That(property.GetMaxLength()).IsEqualTo(40);
        await Assert.That(property.IsUnicode()).IsFalse();
    }

    [Test]
    public async Task String_and_nullable_value_actors_use_the_existing_core_user_contexts()
    {
        using var database = new Database();
        database.SetUser("alice");
        database.SetUser(42);
        using var context = database.CreateContext();
        var text = new StringActorDocument { Id = 1 };
        var number = new IntActorDocument { Id = 2 };
        context.AddRange(text, number);
        await context.SaveChangesAsync();
        context.RemoveRange(text, number);
        await context.SaveChangesAsync();
        await Assert.That((await context.HardDeleteAudits<StringActorDocument, int, string?>().SingleAsync()).DeletedBy).IsEqualTo("alice");
        await Assert.That((await context.HardDeleteAudits<IntActorDocument, int, int?>().SingleAsync()).DeletedBy).IsEqualTo(42);
    }

    [Test]
    public async Task Converted_keys_and_immediate_tracked_cascades_are_audited()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var converted = new ConvertedDocument { Id = new DocumentId(Guid.NewGuid()) };
        var parent = new Parent { Children = [new Child()] };
        var childId = parent.Children[0].Id;
        context.AddRange(converted, parent);
        context.SaveChanges();
        context.RemoveRange(converted, parent);
        context.SaveChanges();
        var id = converted.Id;
        await Assert.That((await context.HardDeleteAudits<ConvertedDocument, DocumentId>().SingleAsync(value => value.EntityKey == id)).EntityKey)
            .IsEqualTo(id);
        await Assert.That((await context.HardDeleteAudits<Parent, Guid>().SingleAsync()).EntityKey).IsEqualTo(parent.Id);
        await Assert.That((await context.HardDeleteAudits<Child, Guid>().SingleAsync()).EntityKey).IsEqualTo(childId);
    }

    [Test]
    public async Task Concurrency_failure_rolls_back_events_and_same_context_retry_creates_exactly_one()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var first = new Document();
        var second = new Document();
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        using (var other = database.CreateContext())
            await other.Set<Document>().Where(value => value.Id == second.Id).ExecuteUpdateAsync(
                setters => setters.SetProperty(value => value.Version, 1));
        context.RemoveRange(first, second);
        await Assert.That(async () => await context.SaveChangesAsync()).Throws<DbUpdateConcurrencyException>();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Set<Document>().AsNoTracking().CountAsync()).IsEqualTo(2);
        context.Entry(second).Property(value => value.Version).OriginalValue = 1;
        await context.SaveChangesAsync();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task Existing_transaction_can_rollback_both_delete_and_audit()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        await context.SaveChangesAsync();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Remove(document);
            await context.SaveChangesAsync();
            await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(1);
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        await Assert.That(await context.Set<Document>().CountAsync()).IsEqualTo(1);
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Unsafe_transaction_configuration_is_rejected_before_writing()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task New_then_removed_entities_and_non_opted_in_entities_have_no_events()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.Remove(document);
        var plain = new PlainDocument();
        context.Add(plain);
        context.SaveChanges();
        context.Remove(plain);
        context.SaveChanges();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        await Assert.That(() => context.HardDeleteAudits<PlainDocument, Guid>()).Throws<InvalidOperationException>();
        await Assert.That(() => context.HardDeleteAudits<Document, int>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Missing_model_configuration_and_actor_providers_fail_explicitly()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        using (var missingActor = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(context.Database.GetDbConnection())
            .AddInterceptors(new HardDeleteAuditInterceptor([])).Options))
        {
            missingActor.Remove(document);
            await Assert.That(() => missingActor.SaveChanges()).Throws<InvalidOperationException>();
        }
        using var missingConvention = new MissingConventionContext(new DbContextOptionsBuilder<MissingConventionContext>()
            .UseSqlite(context.Database.GetDbConnection())
            .AddInterceptors(new HardDeleteAuditInterceptor([])).Options);
        missingConvention.Remove(document);
        await Assert.That(() => missingConvention.SaveChanges()).Throws<InvalidOperationException>();
        await Assert.That(await context.Set<Document>().AsNoTracking().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Ambiguous_model_shapes_and_incomplete_key_mappings_are_rejected()
    {
        var hierarchy = new ModelBuilder();
        hierarchy.Entity<Root>().UseHardDeleteAudit();
        hierarchy.Entity<Derived>().HasBaseType<Root>();
        await Assert.That(() => hierarchy.ApplyHardDeleteAuditConfigurations()).Throws<InvalidOperationException>();

        var sharedTable = new ModelBuilder();
        sharedTable.Entity<Document>().ToTable("Shared").UseHardDeleteAudit();
        sharedTable.Entity<OtherDocument>().ToTable("Shared");
        await Assert.That(() => sharedTable.ApplyHardDeleteAuditConfigurations()).Throws<InvalidOperationException>();

        var keyless = new ModelBuilder();
        keyless.Entity<Document>().HasNoKey().UseHardDeleteAudit();
        await Assert.That(() => keyless.ApplyHardDeleteAuditConfigurations()).Throws<InvalidOperationException>();

        var incomplete = new ModelBuilder();
        incomplete.Entity<CompositeDocument>().HasKey(value => new { value.TenantId, value.Number });
        incomplete.Entity<CompositeDocument>().UseHardDeleteAudit(audit =>
            audit.HasEntityKey<DocumentKey>(key => key.Property(value => value.Number, source => source.Number)));
        await Assert.That(() => incomplete.ApplyHardDeleteAuditConfigurations()).Throws<InvalidOperationException>();

        var duplicates = new HardDeleteAuditKeyBuilder<CompositeDocument, DocumentKey>();
        duplicates.Property(value => value.Number, source => source.Number);
        await Assert.That(() => duplicates.Property(value => value.Number, source => source.Number)).Throws<ArgumentException>();
    }

    [Test]
    public async Task AcceptAllChanges_false_and_cancellation_leave_no_duplicate_pending_events()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        using var cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        await Assert.That(async () => await context.SaveChangesAsync(cancellation.Token)).Throws<OperationCanceledException>();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        context.SaveChanges(acceptAllChangesOnSuccess: false);
        context.ChangeTracker.AcceptAllChanges();
        context.SaveChanges();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Audit_events_cannot_be_manually_modified()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        context.SaveChanges();
        var audit = context.HardDeleteAudits<Document, Guid>().Single();
        audit.DeletedBy = Guid.NewGuid();
        await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Database_write_failure_leaves_neither_deleted_rows_nor_events_and_retry_is_clean()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        var table = context.Model.GetEntityTypes().Single(type => type.ClrType == typeof(HardDeleteAudit<Guid>) &&
            type.Name.StartsWith(typeof(Document).FullName!, StringComparison.Ordinal)).GetTableName()!;
        var delimit = context.GetService<ISqlGenerationHelper>();
        var trigger = $"CREATE TRIGGER FailAudit BEFORE INSERT ON {delimit.DelimitIdentifier(table)} BEGIN SELECT RAISE(ABORT, 'audit unavailable'); END;";
        context.Database.ExecuteSqlRaw(trigger);
        context.Remove(document);
        await Assert.That(async () => await context.SaveChangesAsync()).Throws<DbUpdateException>();
        await Assert.That(await context.Set<Document>().AsNoTracking().CountAsync()).IsEqualTo(1);
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        context.Database.ExecuteSqlRaw("DROP TRIGGER FailAudit;");
        await context.SaveChangesAsync();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Failure_inside_existing_transaction_uses_savepoint_and_allows_retry()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Entry(document).Property(value => value.Version).OriginalValue = -1;
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Remove(document);
        await Assert.That(async () => await context.SaveChangesAsync()).Throws<DbUpdateConcurrencyException>();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(0);
        context.Entry(document).Property(value => value.Version).OriginalValue = 0;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        await Assert.That(await context.HardDeleteAudits<Document, Guid>().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Suppression_keeps_event_but_omits_actor()
    {
        using var database = new Database();
        database.SetUser(Guid.NewGuid());
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        using (StampSuppressor.Suppress())
        {
            context.Remove(document);
            context.SaveChanges();
        }
        await Assert.That(context.HardDeleteAudits<Document, Guid>().Single().DeletedBy).IsNull();
    }

    [Test]
    public async Task Deferred_cascades_and_transactions_without_savepoints_are_rejected_explicitly()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        context.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>();
        context.ChangeTracker.CascadeDeleteTiming = CascadeTiming.Immediate;
        using var transaction = context.Database.BeginTransaction();
        context.Database.AutoSavepointsEnabled = false;
        await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>();
        await Assert.That(context.HardDeleteAudits<Document, Guid>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Automatic_relational_context_configuration_includes_late_table_names_and_design_time()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var services = new ServiceCollection();
        Basilisque.DataAccess.EntityFramework.Relational.IServiceCollectionExtensions.RegisterServices(services);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build());
        services.AddSingleton<IDbContextOptionsConfigurator>(new Configurator(connection));
        services.AddDbContext<AutoContext>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var user = Guid.NewGuid();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = user;
        var context = scope.ServiceProvider.GetRequiredService<AutoContext>();
        context.Database.EnsureCreated();
        var document = new Document();
        context.Add(document);
        context.SaveChanges();
        context.Remove(document);
        context.SaveChanges();
        await Assert.That(context.HardDeleteAudits<Document, Guid>().Single().DeletedBy).IsEqualTo(user);
        await Assert.That(context.Model.GetEntityTypes().Single(type => type.ClrType == typeof(HardDeleteAudit<Guid>)).GetTableName())
            .IsEqualTo("DocumentsDeleteAudit");
        using var design = new DesignFactory(connection).CreateDbContext([]);
        await Assert.That(design.Model.GetEntityTypes().Any(type => type.ClrType == typeof(HardDeleteAudit<Guid>))).IsTrue();
    }

    [Test]
    public async Task Missing_runtime_interceptor_registration_fails_during_context_configuration()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var services = new ServiceCollection();
        Basilisque.DataAccess.EntityFramework.Base.IServiceCollectionExtensions.RegisterServices(services);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build());
        services.AddSingleton<IDbContextOptionsConfigurator>(new Configurator(connection));
        services.AddDbContext<AutoContext>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutoContext>();
        var error = await Assert.That(() => context.Model).Throws<InvalidOperationException>();
        await Assert.That(error.Message.Contains(nameof(HardDeleteAuditInterceptor), StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Migration_snapshot_generation_supports_typed_and_dynamic_key_metadata()
    {
        using var database = new Database();
        using var context = database.CreateContext();
        var services = new ServiceCollection();
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(context);
        var assembly = System.Reflection.Assembly.Load(new AssemblyName(context.GetService<IDatabaseProvider>().Name));
        var designAttribute = assembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>()!;
        var designServices = (IDesignTimeServices)Activator.CreateInstance(assembly.GetType(designAttribute.TypeName, throwOnError: true)!)!;
        designServices.ConfigureDesignTimeServices(services);
        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<IMigrationsCodeGeneratorSelector>().Select("C#");
        var code = generator.GenerateSnapshot("Audit.Tests", typeof(TestContext), "AuditSnapshot",
            context.GetService<IDesignTimeModel>().Model);
        await Assert.That(code.Contains("EntityKey_TenantId", StringComparison.Ordinal)).IsTrue();
        await Assert.That(code.Contains("DynamicDeletions", StringComparison.Ordinal)).IsTrue();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(value => !value.IsDynamic && !string.IsNullOrEmpty(value.Location))
                .Select(value => value.Location))
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AuditSnapshot" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        output.Position = 0;
        var snapshotAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(output);
        var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotAssembly.GetType("Audit.Tests.AuditSnapshot")!)!;
        var snapshotModel = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        await Assert.That(context.GetService<IMigrationsModelDiffer>().HasDifferences(
            snapshotModel.GetRelationalModel(), context.GetService<IDesignTimeModel>().Model.GetRelationalModel())).IsFalse();
    }
}
