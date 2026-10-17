using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Reflection;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>
/// Configures the separate event table for tracked physical deletions.
/// </summary>
/// <typeparam name="TEntity">The audited entity.</typeparam>
public sealed class HardDeleteAuditBuilder<TEntity> where TEntity : class
{
    private readonly EntityTypeBuilder<TEntity> _builder;

    /// <summary>Creates an audit configuration builder for an entity.</summary>
    public HardDeleteAuditBuilder(EntityTypeBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _builder = builder;
        _builder.HasAnnotation(AuditMetadata.Enabled, true);
    }

    /// <summary>Specifies the audit table name and optional schema.</summary>
    public HardDeleteAuditBuilder<TEntity> ToTable(string name, string? schema = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (schema is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        _builder.HasAnnotation(AuditMetadata.Table, name);
        _builder.HasAnnotation(AuditMetadata.Schema, schema);
        return this;
    }

    /// <summary>Specifies the non-nullable actor key type. Stored actors are nullable.</summary>
    public HardDeleteAuditBuilder<TEntity> HasUserKey<TUser>() where TUser : notnull
    {
        _builder.HasAnnotation(AuditMetadata.UserType, typeof(TUser));
        return this;
    }

    /// <summary>Maps a composite primary key into a typed complex value object.</summary>
    public HardDeleteAuditBuilder<TEntity> HasEntityKey<TKey>(Action<HardDeleteAuditKeyBuilder<TEntity, TKey>> configure)
        where TKey : class, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        var keys = new HardDeleteAuditKeyBuilder<TEntity, TKey>();
        configure(keys);
        _builder.HasAnnotation(AuditMetadata.KeyType, typeof(TKey));
        _builder.HasAnnotation(AuditMetadata.KeyMembers, keys.Members.ToArray());
        _builder.HasAnnotation(AuditMetadata.SourceMembers, keys.Sources.ToArray());
        return this;
    }
}

/// <summary>Maps each member of a composite key to a primary-key property.</summary>
/// <typeparam name="TEntity">The audited entity.</typeparam>
/// <typeparam name="TKey">The complex key type, with a public parameterless constructor.</typeparam>
public sealed class HardDeleteAuditKeyBuilder<TEntity, TKey> where TEntity : class where TKey : class, new()
{
    internal List<string> Members { get; } = [];
    internal List<string> Sources { get; } = [];

    /// <summary>Maps a writable key member to a direct entity property of the same type.</summary>
    public HardDeleteAuditKeyBuilder<TEntity, TKey> Property<TProperty>(
        Expression<Func<TKey, TProperty>> keyProperty,
        Expression<Func<TEntity, TProperty>> sourceProperty)
    {
        var member = getProperty(keyProperty);
        if (member.SetMethod is not { IsPublic: true })
            throw new ArgumentException("Audit key members must have public setters.", nameof(keyProperty));
        var source = getProperty(sourceProperty);
        if (Members.Contains(member.Name) || Sources.Contains(source.Name))
            throw new ArgumentException("Each audit key member and source property must be mapped exactly once.");
        Members.Add(member.Name);
        Sources.Add(source.Name);
        return this;
    }

    private static PropertyInfo getProperty<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (expression.Body is not MemberExpression { Member: PropertyInfo property, Expression: ParameterExpression })
            throw new ArgumentException("Select a direct property, not a field or nested expression.", nameof(expression));
        return property;
    }
}
