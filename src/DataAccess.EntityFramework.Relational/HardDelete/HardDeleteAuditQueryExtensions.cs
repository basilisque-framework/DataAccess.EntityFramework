using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>Provides SQL-translatable access to separate physical deletion event tables.</summary>
public static class HardDeleteAuditQueryExtensions
{
    /// <summary>Queries events using a typed key and the default nullable Guid actor.</summary>
    public static IQueryable<HardDeleteAudit<TKey>> HardDeleteAudits<TEntity, TKey>(this DbContext context) where TEntity : class
        => getSet<TEntity, HardDeleteAudit<TKey>>(context);

    /// <summary>Queries events using a typed key and nullable actor type (for example int?).</summary>
    public static IQueryable<HardDeleteAudit<TKey, TUser>> HardDeleteAudits<TEntity, TKey, TUser>(this DbContext context) where TEntity : class
        => getSet<TEntity, HardDeleteAudit<TKey, TUser>>(context);

    /// <summary>
    /// Queries composite-key events without a configured key object.
    /// Use EF.Property with EntityKey_ plus the source key property name.
    /// </summary>
    public static IQueryable<Dictionary<string, object>> HardDeleteAudits<TEntity>(this DbContext context) where TEntity : class
        => getSet<TEntity, Dictionary<string, object>>(context);

    private static DbSet<TAudit> getSet<TEntity, TAudit>(DbContext context) where TEntity : class where TAudit : class
    {
        ArgumentNullException.ThrowIfNull(context);
        var source = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity '{typeof(TEntity)}' is not mapped.");
        var name = source.FindAnnotation(AuditMetadata.AuditName)?.Value as string
            ?? throw new InvalidOperationException($"Hard-delete audit is not configured on '{source.Name}'.");
        var audit = context.Model.FindEntityType(name)!;
        if (audit.ClrType != typeof(TAudit))
            throw new InvalidOperationException($"Audit query type '{typeof(TAudit)}' does not match '{audit.ClrType}'. Select the configured key and nullable actor types, or the dynamic-key overload.");
        return context.Set<TAudit>(name);
    }
}
