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

using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Basilisque.DataAccess.EntityFramework.SqlServer.HardDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Unit.Tests.HardDelete;

public class HardDeleteAuditTemporalTableTests
{
    [Test]
    public async Task Temporal_adapter_returns_same_builder_and_preserves_history_and_period_configuration()
    {
        using var context = new AuditContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(DynamicDocument))!;

        await Assert.That(AuditContext.ReturnedSameBuilder).IsTrue();
        await Assert.That(entity.IsTemporal()).IsTrue();
        await Assert.That(entity.GetHistoryTableName()).IsEqualTo("DynamicDocumentHistory");
        await Assert.That(entity.GetHistoryTableSchema()).IsEqualTo("history");
        await Assert.That(entity.GetPeriodStartPropertyName()).IsEqualTo("ValidFrom");
        await Assert.That(entity.GetPeriodEndPropertyName()).IsEqualTo("ValidTo");
        await Assert.That(entity.FindProperty("ValidFrom")!.GetColumnName()).IsEqualTo("ValidFromUtc");
        await Assert.That(entity.FindProperty("ValidTo")!.GetColumnName()).IsEqualTo("ValidToUtc");
    }

    [Test]
    public async Task Dynamic_key_audit_uses_configured_table_schema_and_actor_type()
    {
        using var context = new AuditContext();
        var table = context.GetService<IDesignTimeModel>().Model.GetRelationalModel()
            .FindTable("DynamicDeletionEvents", "audit")!;

        await Assert.That(table).IsNotNull();
        await Assert.That(table.FindColumn("EntityKey")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(table.FindColumn("DeletedBy")!.ProviderClrType).IsEqualTo(typeof(string));
        await Assert.That(table.FindColumn("DeletedBy")!.IsNullable).IsTrue();
        await Assert.That(table.FindColumn("DeletedAt")!.StoreType).IsEqualTo("datetimeoffset");
        await Assert.That(table.PrimaryKey!.Columns.Single().Name).IsEqualTo("AuditId");
        await Assert.That(table.ForeignKeyConstraints.Any()).IsFalse();
        await Assert.That(table.EntityTypeMappings.All(mapping =>
            mapping.TypeBase is IEntityType entity && !entity.IsTemporal())).IsTrue();
    }

    [Test]
    public async Task Typed_key_audit_maps_the_configured_Guid_key_without_an_entity_relationship()
    {
        using var context = new TypedContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var table = model.GetRelationalModel().FindTable("TypedDeletionEvents", "audit")!;

        await Assert.That(table).IsNotNull();
        await Assert.That(table.FindColumn("EntityKey_Id")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(table.FindColumn("DeletedBy")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(table.FindColumn("DeletedBy")!.IsNullable).IsTrue();
        await Assert.That(table.ForeignKeyConstraints.Any()).IsFalse();
        await Assert.That(model.FindEntityType(typeof(TypedDocument))!.IsTemporal()).IsTrue();
    }

    [Test]
    public async Task Composite_key_without_typed_configuration_uses_dynamic_key_columns()
    {
        using var context = new AuditContext();
        var table = context.GetService<IDesignTimeModel>().Model.GetRelationalModel()
            .FindTable("CompositeDeletionEvents", "audit")!;

        await Assert.That(table.FindColumn("EntityKey_Id")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(table.FindColumn("EntityKey_TenantId")!.ProviderClrType).IsEqualTo(typeof(int));
        await Assert.That(table.FindColumn("EntityKey_Id")!.IsNullable).IsFalse();
        await Assert.That(table.FindColumn("EntityKey_TenantId")!.IsNullable).IsFalse();
        await Assert.That(table.ForeignKeyConstraints.Any()).IsFalse();
    }

    [Test]
    public async Task Marker_enables_separate_audit_table_with_default_Guid_actor()
    {
        using var context = new MarkerContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var tables = model.GetRelationalModel().Tables.ToArray();
        var auditTable = tables.Single(table => table.Name != "MarkerDocuments");

        await Assert.That(tables.Length).IsEqualTo(2);
        await Assert.That(auditTable.FindColumn("EntityKey")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(auditTable.FindColumn("DeletedBy")!.ProviderClrType).IsEqualTo(typeof(Guid));
        await Assert.That(auditTable.FindColumn("DeletedBy")!.IsNullable).IsTrue();
        await Assert.That(model.FindEntityType(typeof(MarkerDocument))!.IsTemporal()).IsTrue();
    }

    [Test]
    public async Task Migration_creates_separate_audit_tables_and_keeps_system_versioning()
    {
        using var context = new AuditContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var operations = context.GetService<IMigrationsModelDiffer>()
            .GetDifferences(null, model.GetRelationalModel());
        var tables = operations.OfType<CreateTableOperation>().ToArray();
        var dynamicAudit = tables.Single(table => table.Name == "DynamicDeletionEvents");
        var sql = string.Join(Environment.NewLine,
            context.GetService<IMigrationsSqlGenerator>().Generate(operations, model)
                .Select(command => command.CommandText));

        await Assert.That(dynamicAudit.Schema).IsEqualTo("audit");
        await Assert.That(dynamicAudit.Columns.Single(column => column.Name == "EntityKey").ColumnType)
            .IsEqualTo("uniqueidentifier");
        await Assert.That(dynamicAudit.Columns.Single(column => column.Name == "DeletedBy").IsNullable).IsTrue();
        await Assert.That(dynamicAudit["SqlServer:IsTemporal"] as bool?).IsNotEqualTo(true);
        await Assert.That(sql).Contains("CREATE TABLE [audit].[DynamicDeletionEvents]");
        await Assert.That(sql).Contains("CREATE TABLE [audit].[CompositeDeletionEvents]");
        await Assert.That(sql).Contains("[EntityKey_TenantId] int NOT NULL");
        await Assert.That(sql).Contains("SYSTEM_VERSIONING = ON");
        await Assert.That(sql).Contains("[history].[DynamicDocumentHistory]");
        await Assert.That(sql).Contains("PERIOD FOR SYSTEM_TIME");
        await Assert.That(sql).Contains("[ValidFromUtc]");
        await Assert.That(sql).Contains("[ValidToUtc]");
    }

    [Test]
    public async Task Typed_key_migration_creates_a_separate_audit_table_and_keeps_system_versioning()
    {
        using var context = new TypedContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var operations = context.GetService<IMigrationsModelDiffer>()
            .GetDifferences(null, model.GetRelationalModel());
        var audit = operations.OfType<CreateTableOperation>()
            .Single(table => table.Name == "TypedDeletionEvents");
        var sql = string.Join(Environment.NewLine,
            context.GetService<IMigrationsSqlGenerator>().Generate(operations, model)
                .Select(command => command.CommandText));

        await Assert.That(audit.Schema).IsEqualTo("audit");
        await Assert.That(audit.Columns.Single(column => column.Name == "EntityKey_Id").ColumnType)
            .IsEqualTo("uniqueidentifier");
        await Assert.That(audit["SqlServer:IsTemporal"] as bool?).IsNotEqualTo(true);
        await Assert.That(sql).Contains("CREATE TABLE [audit].[TypedDeletionEvents]");
        await Assert.That(sql).Contains("SYSTEM_VERSIONING = ON");
        await Assert.That(sql).Contains("[history].[TypedDocumentHistory]");
    }

    [Test]
    public async Task Null_table_builder_is_rejected()
    {
        await Assert.That(() => ((TableBuilder<DynamicDocument>)null!).UseHardDeleteAudit())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Null_combined_table_builder_is_rejected()
    {
        await Assert.That(() => ((TableBuilder<DynamicDocument>)null!).IsTemporalWithHardDeleteAudit())
            .Throws<ArgumentNullException>();
    }

    private sealed class AuditContext : DbContext
    {
        internal static bool ReturnedSameBuilder { get; private set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DynamicDocument>().ToTable("DynamicDocuments", table =>
            {
                ReturnedSameBuilder = ReferenceEquals(table, table.IsTemporalWithHardDeleteAudit(
                    temporal =>
                    {
                        temporal.UseHistoryTable("DynamicDocumentHistory", "history");
                        temporal.HasPeriodStart("ValidFrom").HasColumnName("ValidFromUtc");
                        temporal.HasPeriodEnd("ValidTo").HasColumnName("ValidToUtc");
                    },
                    audit => audit.ToTable("DynamicDeletionEvents", "audit").HasUserKey<string>()));
            });
            modelBuilder.Entity<CompositeDocument>().HasKey(entity => new { entity.TenantId, entity.Id });
            modelBuilder.Entity<CompositeDocument>().ToTable("CompositeDocuments", table =>
            {
                table.IsTemporal();
                table.UseHardDeleteAudit(audit => audit.ToTable("CompositeDeletionEvents", "audit"));
            });
            modelBuilder.ApplyHardDeleteAuditConfigurations();
        }
    }

    private sealed class TypedContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TypedDocument>().ToTable("TypedDocuments", table =>
                table.UseHardDeleteAudit(audit => audit
                    .ToTable("TypedDeletionEvents", "audit")
                    .HasUserKey<Guid>()
                    .HasEntityKey<DocumentKey>(key => key.Property(value => value.DocumentId, entity => entity.Id)))
                .IsTemporal(temporal => temporal.UseHistoryTable("TypedDocumentHistory", "history")));
            modelBuilder.ApplyHardDeleteAuditConfigurations();
        }
    }

    private sealed class MarkerContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarkerDocument>().ToTable("MarkerDocuments", table =>
                table.IsTemporalWithHardDeleteAudit());
            modelBuilder.ApplyHardDeleteAuditConfigurations();
        }
    }

    private sealed class DynamicDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class TypedDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class MarkerDocument : IHardDeleteAudited
    {
        public Guid Id { get; set; }
    }

    private sealed class CompositeDocument
    {
        public int TenantId { get; set; }
        public Guid Id { get; set; }
    }

    public sealed class DocumentKey
    {
        public Guid DocumentId { get; set; }
    }
}
