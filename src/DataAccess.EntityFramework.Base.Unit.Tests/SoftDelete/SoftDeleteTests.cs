/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Basilisque.Core.Auth;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.SoftDelete;

public class SoftDeleteTests
{
    private sealed class Document : ISoftDelete, IStampChanges
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TenantId { get; set; } = 1;
        public bool IsDeleted => DeletedAt.HasValue;
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
        public Details? Details { get; set; }
        public List<Dependent> Dependents { get; set; } = [];
    }

    private sealed class Details
    {
        public string Description { get; set; } = string.Empty;
    }

    private sealed class Dependent
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
    }

    private sealed class StringDocument : ISoftDeleteOfRef<string>
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class LongDocument : ISoftDelete<long>
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public long? DeletedBy { get; set; }
    }

    private sealed class SpecialKeyDocument : ISoftDelete<decimal>
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public decimal? DeletedBy { get; set; }
    }

    private sealed class ShadowDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class GuidShadowDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class RefShadowDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class TimestampShadowDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class AnonymousFilteredDocument : ISoftDeleteTimestamp
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int TenantId { get; set; } = 1;
    }

    private sealed class PlainDocument
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
    }

    private sealed class InvalidOwner
    {
        public Guid Id { get; set; }
        public OwnedSoftDelete Child { get; set; } = new();
    }

    private sealed class OwnedSoftDelete : ISoftDeleteTimestamp
    {
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private class NonSoftDeleteRoot
    {
        public Guid Id { get; set; }
    }

    private sealed class InvalidDerivedSoftDelete : NonSoftDeleteRoot, ISoftDeleteTimestamp
    {
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class CascadeParent : ISoftDeleteTimestamp
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public List<CascadeChild> Children { get; set; } = [];
    }

    private sealed class CascadeChild : ISoftDeleteTimestamp
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class NullParent : ISoftDeleteTimestamp
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public List<NullChild> Children { get; set; } = [];
    }

    private sealed class NullChild
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
    }

    private class InheritedDocument : ISoftDeleteTimestamp
    {
        public Guid Id { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class DerivedDocument : InheritedDocument, ISoftDelete<long>
    {
        public string Name { get; set; } = string.Empty;
        public long? DeletedBy { get; set; }
    }

    private sealed class TestContext(IDbProviderServiceProvider serviceProvider) : BaseDbContext<TestContext>(serviceProvider)
    {
        public int TenantId { get; set; } = 1;
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<StringDocument> Strings => Set<StringDocument>();
        public DbSet<LongDocument> Longs => Set<LongDocument>();
        public DbSet<ShadowDocument> Shadows => Set<ShadowDocument>();
        public DbSet<AnonymousFilteredDocument> Anonymous => Set<AnonymousFilteredDocument>();
        public DbSet<PlainDocument> Plain => Set<PlainDocument>();
        public DbSet<CascadeParent> CascadeParents => Set<CascadeParent>();
        public DbSet<NullParent> NullParents => Set<NullParent>();
        public DbSet<DerivedDocument> Derived => Set<DerivedDocument>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Document>().HasQueryFilter("Tenant", document => document.TenantId == TenantId);
            modelBuilder.Entity<Document>().OwnsOne(document => document.Details);
            modelBuilder.Entity<Document>().HasMany(document => document.Dependents).WithOne()
                .HasForeignKey(dependent => dependent.DocumentId).OnDelete(DeleteBehavior.ClientNoAction);
            modelBuilder.Entity<CascadeParent>().HasMany(parent => parent.Children).WithOne()
                .HasForeignKey(child => child.ParentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<NullParent>().HasMany(parent => parent.Children).WithOne()
                .HasForeignKey(child => child.ParentId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<ShadowDocument>()
                .UseShadowCreationStamp<ShadowDocument, int>()
                .UseShadowModificationStamp<ShadowDocument, int>()
                .UseShadowSoftDelete<ShadowDocument, int>();
            modelBuilder.Entity<GuidShadowDocument>().UseShadowSoftDelete();
            modelBuilder.Entity<RefShadowDocument>().UseShadowSoftDeleteOfRef<RefShadowDocument, string>();
            modelBuilder.Entity<TimestampShadowDocument>().UseShadowSoftDeleteTimestamp();
            modelBuilder.Entity<AnonymousFilteredDocument>().HasQueryFilter(document => document.TenantId == 1);
        }
    }

    private sealed class SqliteConfigurator(SqliteConnection connection) : IDbContextOptionsConfigurator
    {
        public void Configure<TDbContext>(BaseDbContext<TDbContext> context, DbContextOptionsBuilder optionsBuilder)
            where TDbContext : BaseDbContext<TDbContext>
        {
            optionsBuilder.UseSqlite(connection);
        }
    }

    private sealed class StandaloneContext(DbContextOptions<StandaloneContext> options) : DbContext(options)
    {
        public DbSet<StringDocument> Documents => Set<StringDocument>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySoftDeleteConfigurations();
        }
    }

    private sealed class SpecialKeyContext(DbContextOptions<SpecialKeyContext> options) : DbContext(options)
    {
        public DbSet<SpecialKeyDocument> Documents => Set<SpecialKeyDocument>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySoftDeleteConfigurations();
        }
    }

    private sealed class DesignTimeFactory(SqliteConnection connection) : BaseDesignTimeDbContextFactory<TestContext>
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build();
        }

        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddSingleton<IDbContextOptionsConfigurator>(new SqliteConfigurator(connection));
            var info = Substitute.For<IDbProviderInfo>();
            info.ProviderKey.Returns("SQLite");
            info.ProviderName.Returns("SQLite");
            services.AddSingleton(info);
        }
    }

    private sealed class Database : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
        private readonly ServiceProvider _provider;

        public Database(Action<IServiceCollection>? configureServices = null)
        {
            _connection.Open();
            var services = new ServiceCollection();
            Basilisque.DataAccess.EntityFramework.Base.IServiceCollectionExtensions.RegisterServices(services);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Database:Provider"] = "SQLite" }).Build());
            services.AddSingleton<IDbContextOptionsConfigurator>(new SqliteConfigurator(_connection));
            services.AddDbContext<TestContext>();
            configureServices?.Invoke(services);
            _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            try
            {
                using var scope = CreateScope();
                scope.ServiceProvider.GetRequiredService<TestContext>().Database.EnsureCreated();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public void Dispose()
        {
            try
            {
                _provider.Dispose();
            }
            finally
            {
                _connection.Dispose();
            }
        }
    }

    [Test]
    public async Task Default_interface_flag_is_calculated_only_from_the_date()
    {
        ISoftDeleteTimestamp document = new CascadeParent();
        await Assert.That(document.IsDeleted).IsFalse();
        document.DeletedAt = DateTimeOffset.UtcNow;
        await Assert.That(document.IsDeleted).IsTrue();
        document.DeletedAt = null;
        await Assert.That(document.IsDeleted).IsFalse();
    }

    [Test]
    public async Task Active_rows_store_null_user_keys_in_the_database_and_no_flag_column()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var services = scope.ServiceProvider;
        services.GetRequiredService<IWritableUserContext<Guid>>().UserId = Guid.NewGuid();
        services.GetRequiredService<IWritableUserContext<string>>().UserId = "current-user";
        services.GetRequiredService<IWritableUserContext<long>>().UserId = 123;
        services.GetRequiredService<IWritableUserContext<int>>().UserId = 42;
        var context = services.GetRequiredService<TestContext>();
        var shadow = new ShadowDocument();
        context.AddRange(new Document { DeletedBy = Guid.Empty },
            new StringDocument { DeletedBy = string.Empty }, new LongDocument { DeletedBy = 0 }, shadow);
        context.Entry(shadow).Property<int?>("DeletedBy").CurrentValue = 0;
        await context.SaveChangesAsync();

        foreach (var type in new[] { typeof(Document), typeof(StringDocument), typeof(LongDocument), typeof(ShadowDocument) })
        {
            var entityType = context.Model.FindEntityType(type) ?? throw new InvalidOperationException("Missing test entity.");
            await Assert.That(entityType.FindProperty("IsDeleted")).IsNull();
            await Assert.That(entityType.FindProperty("DeletedBy")!.IsNullable).IsTrue();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT \"DeletedBy\" FROM \"{entityType.GetTableName()}\"";
            await Assert.That(await command.ExecuteScalarAsync()).IsEqualTo(DBNull.Value);
        }
    }

    [Test]
    public async Task Shadow_defaults_reference_keys_and_timestamp_only_configuration_work()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var userId = Guid.NewGuid();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = userId;
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<string>>().UserId = "shadow-user";
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var guid = new GuidShadowDocument();
        var reference = new RefShadowDocument();
        var timestampOnly = new TimestampShadowDocument();
        context.AddRange(guid, reference, timestampOnly);
        await context.SaveChangesAsync();
        await Assert.That(context.Entry(guid).Property<Guid?>("DeletedBy").CurrentValue).IsNull();
        await Assert.That(context.Entry(reference).Property<string?>("DeletedBy").CurrentValue).IsNull();
        await Assert.That(context.Entry(timestampOnly).Metadata.FindProperty("DeletedBy")).IsNull();
        context.RemoveRange(guid, reference, timestampOnly);
        await context.SaveChangesAsync();

        await Assert.That(context.Entry(guid).Property<Guid?>("DeletedBy").CurrentValue).IsEqualTo(userId);
        await Assert.That(context.Entry(reference).Property<string?>("DeletedBy").CurrentValue).IsEqualTo("shadow-user");
        await Assert.That(context.Entry(timestampOnly).Property<DateTimeOffset?>("DeletedAt").CurrentValue).IsNotNull();
        await Assert.That(await context.Set<GuidShadowDocument>().CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Set<RefShadowDocument>().CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Set<TimestampShadowDocument>().CountAsync()).IsEqualTo(0);
        context.Entry(reference).Property<DateTimeOffset?>("DeletedAt").CurrentValue = null;
        using (StampSuppressor.Suppress())
            await context.SaveChangesAsync();
        await Assert.That(context.Entry(reference).Property<string?>("DeletedBy").CurrentValue).IsNull();
        await Assert.That(await context.Set<RefShadowDocument>().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Remove_updates_deletion_and_modification_stamps_and_preserves_the_row()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var user = scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>();
        user.UserId = Guid.NewGuid();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new Document { Name = "Original" };
        context.Add(document);
        await context.SaveChangesAsync();
        var createdAt = document.CreatedAt;
        var createdBy = document.CreatedBy;
        var deletingUser = Guid.NewGuid();
        user.UserId = deletingUser;
        context.Remove(document);

        await context.SaveChangesAsync();

        await Assert.That(document.IsDeleted).IsTrue();
        await Assert.That(document.DeletedAt).IsEqualTo(document.ModifiedAt);
        await Assert.That(document.DeletedAt!.Value.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(document.DeletedBy).IsEqualTo(deletingUser);
        await Assert.That(document.ModifiedBy).IsEqualTo(deletingUser);
        await Assert.That(document.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(document.CreatedBy).IsEqualTo(createdBy);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Detached_stub_deletion_does_not_overwrite_other_columns()
    {
        using var database = new Database();
        var document = new Document { Name = "Keep this name" };
        using (var scope = database.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestContext>();
            context.Add(document);
            await context.SaveChangesAsync();
        }

        using (var scope = database.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestContext>();
            context.Remove(new Document { Id = document.Id });
            context.SaveChanges();
        }

        using var verificationScope = database.CreateScope();
        var stored = await verificationScope.ServiceProvider.GetRequiredService<TestContext>()
            .Documents.IgnoreQueryFilters().SingleAsync();
        await Assert.That(stored.IsDeleted).IsTrue();
        await Assert.That(stored.Name).IsEqualTo("Keep this name");
        await Assert.That(stored.CreatedAt).IsEqualTo(document.CreatedAt);
    }

    [Test]
    public async Task RemoveRange_supports_string_long_and_shadow_int_user_keys()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var services = scope.ServiceProvider;
        services.GetRequiredService<IWritableUserContext<string>>().UserId = "deleting-user";
        services.GetRequiredService<IWritableUserContext<long>>().UserId = 5_000_000_000;
        services.GetRequiredService<IWritableUserContext<int>>().UserId = 42;
        var context = services.GetRequiredService<TestContext>();
        var stringDocument = new StringDocument();
        var longDocument = new LongDocument();
        var shadow = new ShadowDocument { Name = "Shadow" };
        context.AddRange(stringDocument, longDocument, shadow);
        context.SaveChanges();
        var createdAt = context.Entry(shadow).Property<DateTimeOffset>("CreatedAt").CurrentValue;
        context.RemoveRange(stringDocument, longDocument, shadow);

        await context.SaveChangesAsync();

        await Assert.That(stringDocument.DeletedBy).IsEqualTo("deleting-user");
        await Assert.That(longDocument.DeletedBy).IsEqualTo(5_000_000_000);
        await Assert.That(context.Entry(shadow).Property<int?>("DeletedBy").CurrentValue).IsEqualTo(42);
        await Assert.That(context.Entry(shadow).Property<DateTimeOffset?>("DeletedAt").CurrentValue)
            .IsEqualTo(context.Entry(shadow).Property<DateTimeOffset>("ModifiedAt").CurrentValue);
        await Assert.That(context.Entry(shadow).Property<DateTimeOffset>("CreatedAt").CurrentValue).IsEqualTo(createdAt);
        await Assert.That(await context.Shadows.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Shadows.IgnoreQueryFilters().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Missing_current_user_leaves_the_nullable_deletion_user_empty()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var previousUser = Guid.NewGuid();
        var document = new Document { DeletedBy = previousUser };
        context.Add(document);
        await context.SaveChangesAsync();
        await Assert.That(document.DeletedBy).IsNull();
        context.Remove(document);
        await context.SaveChangesAsync();

        await Assert.That(document.DeletedAt).IsNotNull();
        await Assert.That(document.DeletedBy).IsNull();
    }

    [Test]
    public async Task Named_soft_delete_filter_can_be_disabled_without_disabling_tenant_filter()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        context.AddRange(new Document(), new Document { DeletedAt = DateTimeOffset.UtcNow }, new Document { TenantId = 2 });
        await context.SaveChangesAsync();

        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.Documents.IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName]).CountAsync()).IsEqualTo(2);
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(3);
        context.TenantId = 2;
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.Documents.IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName]).CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task New_entities_marked_deleted_preserve_the_timestamp_and_receive_the_deletion_user()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var userId = Guid.NewGuid();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = userId;
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var fixedTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var document = new Document { DeletedAt = fixedTime };
        context.Add(document);

        await context.SaveChangesAsync();

        await Assert.That(document.DeletedAt).IsEqualTo(fixedTime);
        await Assert.That(document.DeletedBy).IsEqualTo(userId);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Anonymous_application_filter_is_preserved()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        context.AddRange(new AnonymousFilteredDocument(), new AnonymousFilteredDocument { DeletedAt = DateTimeOffset.UtcNow },
            new AnonymousFilteredDocument { TenantId = 2 });
        await context.SaveChangesAsync();

        await Assert.That(await context.Anonymous.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.Anonymous.IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName]).CountAsync()).IsEqualTo(2);
        await Assert.That(await context.Anonymous.IgnoreQueryFilters().CountAsync()).IsEqualTo(3);
    }

    [Test]
    public async Task Hard_delete_bypass_is_explicit_nested_and_independent_of_query_filters()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new Document();
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();

        using (SoftDeleteSuppressor.AllowHardDelete())
        {
            using (SoftDeleteSuppressor.AllowHardDelete())
            {
                await Task.Yield();
                await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
                context.Remove(document);
                await context.SaveChangesAsync();
            }

            await Assert.That(SoftDeleteSuppressor.IsHardDeleteAllowed).IsTrue();
        }

        await Assert.That(SoftDeleteSuppressor.IsHardDeleteAllowed).IsFalse();
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Stamp_suppression_does_not_enable_physical_deletion()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new Document();
        context.Add(document);
        await context.SaveChangesAsync();
        var modifiedAt = document.ModifiedAt;

        using (StampSuppressor.Suppress())
        {
            context.Remove(document);
            await context.SaveChangesAsync();
        }

        await Assert.That(document.IsDeleted).IsTrue();
        await Assert.That(document.DeletedAt).IsNotNull();
        await Assert.That(document.DeletedBy).IsNull();
        await Assert.That(document.ModifiedAt).IsEqualTo(modifiedAt);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Repeated_soft_delete_does_not_change_existing_deletion_audit()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new Document();
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();
        var deletedAt = document.DeletedAt;
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = Guid.NewGuid();
        context.Remove(document);

        await context.SaveChangesAsync();

        await Assert.That(document.DeletedAt).IsEqualTo(deletedAt);
        await Assert.That(document.DeletedBy).IsNull();
    }

    [Test]
    public async Task Non_opted_in_entities_are_still_physically_deleted()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new PlainDocument();
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();

        await Assert.That(await context.Plain.IgnoreQueryFilters().CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Setting_the_date_directly_stamps_the_user_and_restore_clears_both_deletion_fields()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var userId = Guid.NewGuid();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = userId;
        var document = new Document();
        context.Add(document);
        await context.SaveChangesAsync();
        var fixedTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        document.DeletedAt = fixedTime;
        await context.SaveChangesAsync();
        await Assert.That(document.DeletedAt).IsEqualTo(fixedTime);
        await Assert.That(document.DeletedBy).IsEqualTo(userId);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
        var deletionModifiedAt = document.ModifiedAt;

        document.DeletedAt = null;
        await context.SaveChangesAsync();

        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(1);
        await Assert.That(document.DeletedAt).IsNull();
        await Assert.That(document.DeletedBy).IsNull();
        await Assert.That(document.ModifiedAt).IsGreaterThan(deletionModifiedAt);
        await Assert.That(document.ModifiedBy).IsEqualTo(userId);
    }

    [Test]
    public async Task Owned_values_and_no_action_dependents_remain_intact()
    {
        using var database = new Database();
        var document = new Document
        {
            Details = new Details { Description = "Keep owned data" },
            Dependents = [new Dependent { Id = Guid.NewGuid() }]
        };
        using (var scope = database.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestContext>();
            context.Add(document);
            await context.SaveChangesAsync();
            context.Remove(document);
            await context.SaveChangesAsync();
        }

        using var verification = database.CreateScope();
        var stored = await verification.ServiceProvider.GetRequiredService<TestContext>().Documents
            .IgnoreQueryFilters().Include(entity => entity.Dependents).SingleAsync();
        await Assert.That(stored.Details!.Description).IsEqualTo("Keep owned data");
        await Assert.That(stored.Dependents.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Cascades_are_rejected_before_deleting_or_soft_deleting_dependents(bool deferred)
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var parent = new CascadeParent { Children = [new CascadeChild()] };
        context.Add(parent);
        await context.SaveChangesAsync();
        if (deferred)
            context.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        context.Remove(parent);

        var exception = await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>()
            ?? throw new InvalidOperationException("Expected cascade validation to fail.");
        await Assert.That(exception.Message).Contains("No automatic soft-delete cascade");

        using var verification = database.CreateScope();
        var stored = await verification.ServiceProvider.GetRequiredService<TestContext>()
            .CascadeParents.IgnoreQueryFilters().Include(entity => entity.Children).SingleAsync();
        await Assert.That(stored.DeletedAt).IsNull();
        await Assert.That(stored.Children.Single().DeletedAt).IsNull();
    }

    [Test]
    public async Task Cascaded_foreign_key_nulling_is_rejected_instead_of_severing_relationships()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var parent = new NullParent { Children = [new NullChild()] };
        context.Add(parent);
        await context.SaveChangesAsync();
        context.Remove(parent);

        await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>();

        using var verification = database.CreateScope();
        var stored = await verification.ServiceProvider.GetRequiredService<TestContext>()
            .NullParents.Include(entity => entity.Children).SingleAsync();
        await Assert.That(stored.Children.Single().ParentId).IsEqualTo(parent.Id);
    }

    [Test]
    public async Task Explicitly_disabled_cascades_are_not_forced_and_children_remain_visible()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var parent = new CascadeParent { Children = [new CascadeChild()] };
        context.Add(parent);
        await context.SaveChangesAsync();
        context.ChangeTracker.CascadeDeleteTiming = CascadeTiming.Never;
        context.Remove(parent);

        await context.SaveChangesAsync();

        await Assert.That(parent.DeletedAt).IsNotNull();
        await Assert.That(parent.Children.Single().DeletedAt).IsNull();
        await Assert.That(await context.Set<CascadeChild>().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Inheritance_uses_the_root_filter_and_preserves_the_derived_row()
    {
        using var database = new Database();
        using var scope = database.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<long>>().UserId = 99;
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();
        var document = new DerivedDocument { Name = "Derived" };
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();

        await Assert.That(await context.Derived.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Derived.IgnoreQueryFilters().CountAsync()).IsEqualTo(1);
        await Assert.That(document.DeletedAt).IsNotNull();
        await Assert.That(document.DeletedBy).IsEqualTo(99);
    }

    [Test]
    public async Task Standalone_context_supports_explicit_model_and_interceptor_configuration()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var user = new WritableUserContext<string> { UserId = "standalone" };
        var interceptor = new CompositeStampInterceptor([new TimestampStampHandler(), new UserStampHandler<string>(user)]);
        var options = new DbContextOptionsBuilder<StandaloneContext>().UseSqlite(connection).AddInterceptors(interceptor).Options;
        await using var context = new StandaloneContext(options);
        await context.Database.EnsureCreatedAsync();
        var document = new StringDocument();
        context.Add(document);
        await context.SaveChangesAsync();
        context.Remove(document);
        await context.SaveChangesAsync();

        await Assert.That(document.DeletedBy).IsEqualTo("standalone");
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Deletion_users_are_isolated_between_context_scopes()
    {
        using var database = new Database();
        using var firstScope = database.CreateScope();
        using var secondScope = database.CreateScope();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        firstScope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = firstUser;
        secondScope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = secondUser;
        var firstContext = firstScope.ServiceProvider.GetRequiredService<TestContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestContext>();
        var first = new Document();
        var second = new Document();
        firstContext.AddRange(first, second);
        await firstContext.SaveChangesAsync();
        firstContext.Remove(first);
        await firstContext.SaveChangesAsync();
        var secondStub = new Document { Id = second.Id };
        secondContext.Remove(secondStub);
        await secondContext.SaveChangesAsync();

        await Assert.That(first.DeletedBy).IsEqualTo(firstUser);
        await Assert.That(secondStub.DeletedBy).IsEqualTo(secondUser);
    }

    [Test]
    public async Task Design_time_keeps_filters_and_soft_delete_but_preserves_explicit_audit_values()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new DesignTimeFactory(connection);
        await using var context = factory.CreateDbContext([]);
        await context.Database.EnsureCreatedAsync();
        var fixedTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fixedUser = Guid.NewGuid();
        var document = new Document
        {
            DeletedAt = fixedTime, DeletedBy = fixedUser,
            CreatedAt = fixedTime, CreatedBy = fixedUser,
            ModifiedAt = fixedTime, ModifiedBy = fixedUser
        };
        var activeDocument = new Document
        {
            CreatedAt = fixedTime, CreatedBy = fixedUser,
            ModifiedAt = fixedTime, ModifiedBy = fixedUser
        };
        context.AddRange(document, activeDocument);
        await context.SaveChangesAsync();
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(1);
        context.Remove(activeDocument);
        context.Remove(document);
        await context.SaveChangesAsync();

        await Assert.That(document.IsDeleted).IsTrue();
        await Assert.That(document.DeletedAt).IsEqualTo(fixedTime);
        await Assert.That(document.DeletedBy).IsEqualTo(fixedUser);
        await Assert.That(document.ModifiedAt).IsEqualTo(fixedTime);
        await Assert.That(activeDocument.DeletedAt).IsNotNull();
        await Assert.That(activeDocument.DeletedBy).IsNull();
        await Assert.That(activeDocument.ModifiedAt).IsEqualTo(fixedTime);
        await Assert.That(await context.Documents.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.Documents.IgnoreQueryFilters().CountAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task Owned_entities_cannot_independently_opt_in()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<InvalidOwner>().HasKey(owner => owner.Id);
        modelBuilder.Entity<InvalidOwner>().OwnsOne(owner => owner.Child, owned => owned.Property(child => child.DeletedAt));

        var exception = await Assert.That(() => modelBuilder.ApplySoftDeleteConfigurations()).Throws<InvalidOperationException>()
            ?? throw new InvalidOperationException("Expected owned-entity validation to fail.");
        await Assert.That(exception.Message).Contains("non-owned root");
    }

    [Test]
    public async Task Derived_entities_cannot_opt_in_without_their_root()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<NonSoftDeleteRoot>().HasKey(entity => entity.Id);
        modelBuilder.Entity<InvalidDerivedSoftDelete>().HasBaseType<NonSoftDeleteRoot>().Property(entity => entity.DeletedAt);

        var exception = await Assert.That(() => modelBuilder.ApplySoftDeleteConfigurations()).Throws<InvalidOperationException>()
            ?? throw new InvalidOperationException("Expected root-entity validation to fail.");
        await Assert.That(exception.Message).Contains("root entity");
    }

    [Test]
    public async Task Ignored_audit_properties_are_reported_instead_of_silently_omitted()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<StringDocument>().Property(document => document.DeletedAt);
        modelBuilder.Entity<StringDocument>().Ignore(document => document.DeletedBy);

        var exception = await Assert.That(() => modelBuilder.ApplySoftDeleteConfigurations()).Throws<InvalidOperationException>()
            ?? throw new InvalidOperationException("Expected missing-property validation to fail.");
        await Assert.That(exception.Message).Contains("DeletedBy");
        await Assert.That(exception.Message).Contains("must be mapped");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Custom_deletion_key_types_require_an_explicit_handler(bool registerHandler)
    {
        using var database = new Database(services =>
        {
            if (registerHandler)
                services.AddScoped<IUserStampHandler>(sp =>
                    new UserStampHandler<decimal>(sp.GetRequiredService<IUserContext<decimal>>()));
        });
        using var scope = database.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWritableUserContext<decimal>>().UserId = 123m;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var optionsBuilder = new DbContextOptionsBuilder<SpecialKeyContext>().UseSqlite(connection);
        optionsBuilder.UseEFCoreStamping(scope.ServiceProvider);
        await using var context = new SpecialKeyContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();
        var document = new SpecialKeyDocument { Id = Guid.NewGuid() };
        context.Add(document);
        using (StampSuppressor.Suppress())
            await context.SaveChangesAsync();

        context.Remove(document);
        if (registerHandler)
        {
            await context.SaveChangesAsync();
            await Assert.That(document.DeletedBy).IsEqualTo(123m);
            await Assert.That(document.DeletedAt).IsNotNull();
        }
        else
        {
            var exception = await Assert.That(() => context.SaveChanges()).Throws<InvalidOperationException>()
                ?? throw new InvalidOperationException("Expected a missing-handler exception.");
            await Assert.That(exception.Message).Contains(typeof(decimal).ToString());
            await Assert.That((await context.Documents.AsNoTracking().SingleAsync()).DeletedAt).IsNull();
        }
    }
}
