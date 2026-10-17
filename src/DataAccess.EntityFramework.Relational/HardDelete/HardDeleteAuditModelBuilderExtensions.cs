using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>Configures physical deletion events independently of a database provider.</summary>
public static class HardDeleteAuditModelBuilderExtensions
{
    /// <summary>Enables auditing and optionally overrides its defaults.</summary>
    public static EntityTypeBuilder<TEntity> UseHardDeleteAudit<TEntity>(this EntityTypeBuilder<TEntity> builder,
        Action<HardDeleteAuditBuilder<TEntity>>? configure = null) where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        var audit = new HardDeleteAuditBuilder<TEntity>(builder);
        configure?.Invoke(audit);
        return builder;
    }

    /// <summary>
    /// Registers late audit model configuration for ordinary DbContexts.
    /// Relational BaseDbContext installs this convention automatically.
    /// </summary>
    public static void AddHardDeleteAuditConvention(this ModelConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Conventions.Add(_ => new HardDeleteAuditConvention());
    }

    /// <summary>Applies audit configuration after all application model configuration.</summary>
    public static void ApplyHardDeleteAuditConfigurations(this ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        applyConfigurations(builder.Model);
    }

    internal static void applyConfigurations(IMutableModel model)
    {
        using var conventions = model.DelayConventions();
        foreach (var entity in model.GetEntityTypes().ToArray())
        {
            if (entity.FindAnnotation(AuditMetadata.SourceEntity) is not null)
                continue;
            var markers = entity.ClrType.GetInterfaces().Where(type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHardDeleteAudited<>)).ToArray();
            if (entity.FindAnnotation(AuditMetadata.Enabled)?.Value is not true && markers.Length == 0)
                continue;
            if (entity.FindAnnotation(AuditMetadata.AuditName) is not null)
                continue;
            if (markers.Length > 1 && entity.FindAnnotation(AuditMetadata.UserType) is null)
                throw new InvalidOperationException($"Entity '{entity.Name}' declares multiple audit actor types. Select one with HasUserKey.");

            var key = entity.FindPrimaryKey()
                ?? throw new InvalidOperationException($"Hard-delete audit requires a primary key on '{entity.Name}'.");
            if (entity.IsOwned() || entity.BaseType is not null || entity.GetDerivedTypes().Any() ||
                entity.HasSharedClrType || entity.GetMappingFragments().Any() || entity.GetTableName() is null)
                throw new InvalidOperationException($"Hard-delete audit requires a non-owned, non-shared, single-table entity without inheritance: '{entity.Name}'.");
            if (model.GetEntityTypes().Any(other => other != entity &&
                other.GetTableName() == entity.GetTableName() && other.GetSchema() == entity.GetSchema()))
                throw new InvalidOperationException($"Hard-delete audit does not support table sharing for '{entity.Name}'.");

            var actor = (Type?)entity.FindAnnotation(AuditMetadata.UserType)?.Value ??
                markers.SingleOrDefault()?.GenericTypeArguments[0] ?? typeof(Guid);
            if (Nullable.GetUnderlyingType(actor) is not null)
                throw new InvalidOperationException("HasUserKey expects a non-nullable actor key type; audit actors are made nullable automatically.");
            entity.SetAnnotation(AuditMetadata.UserType, actor);
            var actorPropertyType = actor.IsValueType ? typeof(Nullable<>).MakeGenericType(actor) : actor;
            var complexKey = (Type?)entity.FindAnnotation(AuditMetadata.KeyType)?.Value;
            var dynamicKey = key.Properties.Count > 1 && complexKey is null;
            var keyType = complexKey ?? key.Properties[0].ClrType;
            var auditClrType = dynamicKey ? typeof(Dictionary<string, object>) :
                actor == typeof(Guid) ? typeof(HardDeleteAudit<>).MakeGenericType(keyType) :
                typeof(HardDeleteAudit<,>).MakeGenericType(keyType, actorPropertyType);
            var name = entity.Name + ".HardDeleteAudit";
            var table = (string?)entity.FindAnnotation(AuditMetadata.Table)?.Value ?? entity.GetTableName() + "DeleteAudit";
            var schema = entity.FindAnnotation(AuditMetadata.Table) is not null
                ? (string?)entity.FindAnnotation(AuditMetadata.Schema)?.Value : entity.GetSchema();
            if (model.GetEntityTypes().Any(other => other.GetTableName() == table && other.GetSchema() == schema))
                throw new InvalidOperationException($"Hard-delete audit table '{schema}.{table}' is already mapped.");
            var audit = model.AddEntityType(name, auditClrType);
            audit.SetTableName(table);
            audit.SetSchema(schema);
            audit.SetAnnotation(AuditMetadata.SourceEntity, entity.Name);
            var id = addProperty(audit, "AuditId", typeof(Guid));
            id.ValueGenerated = ValueGenerated.Never;
            audit.SetPrimaryKey([id]);
            addProperty(audit, "DeletedAt", typeof(DateTimeOffset));
            var actorProperty = addProperty(audit, "DeletedBy", actorPropertyType);
            actorProperty.IsNullable = true;
            if (actor == typeof(string))
                actorProperty.SetMaxLength(256);

            var sources = key.Properties.Select(property => property.Name).ToArray();
            string[] members;
            if (complexKey is not null)
            {
                sources = (string[]?)entity.FindAnnotation(AuditMetadata.SourceMembers)?.Value ?? [];
                members = (string[]?)entity.FindAnnotation(AuditMetadata.KeyMembers)?.Value ?? [];
                if (sources.Length != key.Properties.Count || !sources.ToHashSet().SetEquals(key.Properties.Select(property => property.Name)) ||
                    members.Length != sources.Length || complexKey.GetProperties().Count(property => property.GetMethod?.IsPublic is true) != members.Length)
                    throw new InvalidOperationException($"Map every primary-key component of '{entity.Name}' exactly once, and no additional key-object members.");
                var complex = audit.AddComplexProperty(auditClrType.GetProperty("EntityKey")!, collection: false);
                complex.IsNullable = false;
                for (var index = 0; index < sources.Length; index++)
                {
                    var source = entity.FindProperty(sources[index])!;
                    var property = addProperty(complex.ComplexType, members[index], source.ClrType);
                    copyKeyMapping(source, property);
                    property.SetColumnName(AuditMetadata.DynamicKeyPrefix + sources[index]);
                }
            }
            else
            {
                members = dynamicKey ? sources.Select(source => AuditMetadata.DynamicKeyPrefix + source).ToArray() : ["EntityKey"];
                for (var index = 0; index < sources.Length; index++)
                    copyKeyMapping(key.Properties[index], addProperty(audit, members[index], key.Properties[index].ClrType));
                audit.AddIndex(members.Select(member => audit.FindProperty(member)!).ToArray());
            }
            entity.SetAnnotation(AuditMetadata.AuditName, name);
            entity.SetAnnotation(AuditMetadata.SourceMembers, sources);
            entity.SetAnnotation(AuditMetadata.KeyMembers, members);
        }
    }

    private static IMutableProperty addProperty(IMutableTypeBase type, string name, Type clrType)
    {
        return type.FindProperty(name) ?? (type.ClrType == typeof(Dictionary<string, object>)
            ? type.AddIndexerProperty(name, clrType)
            : type.AddProperty(name, clrType, type.ClrType.GetProperty(name)
                ?? throw new InvalidOperationException($"Audit member '{type.ClrType}.{name}' is missing.")));
    }

    private static void copyKeyMapping(IMutableProperty source, IMutableProperty target)
    {
        target.IsNullable = false;
        target.ValueGenerated = ValueGenerated.Never;
        if (source.GetValueConverter() is { } converter)
            target.SetValueConverter(converter);
        if (source.GetValueComparer() is { } comparer)
            target.SetValueComparer(comparer);
        if (source.GetMaxLength() is { } length)
            target.SetMaxLength(length);
        if (source.IsUnicode() is { } unicode)
            target.SetIsUnicode(unicode);
        if (source.GetPrecision() is { } precision)
            target.SetPrecision(precision);
        if (source.GetScale() is { } scale)
            target.SetScale(scale);
        if (source.GetColumnType() is { } columnType)
            target.SetColumnType(columnType);
    }
}

internal sealed class HardDeleteAuditConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        if (modelBuilder.Metadata is not IMutableModel model)
            throw new InvalidOperationException("Hard-delete audit requires a mutable model during finalization.");
        HardDeleteAuditModelBuilderExtensions.applyConfigurations(model);
    }
}
