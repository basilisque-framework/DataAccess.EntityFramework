using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>
/// Adds physical deletion events to the same SaveChanges transaction.
/// Must run after the soft-delete/stamping interceptor.
/// </summary>
[RegisterServiceScoped]
public sealed class HardDeleteAuditInterceptor(IEnumerable<IHardDeleteAuditUserProvider> userProviders) : SaveChangesInterceptor
{
    private readonly Dictionary<Type, IHardDeleteAuditUserProvider> _users = userProviders
        .GroupBy(provider => provider.UserKeyType).ToDictionary(group => group.Key, group => group.Last());
    private readonly ConditionalWeakTable<DbContext, List<EntityEntry>> _pending = new();
    private static readonly MethodInfo _addMethod = typeof(HardDeleteAuditInterceptor)
        .GetMethod(nameof(add), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        prepare(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        prepare(eventData.Context);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        cleanup(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        cleanup(eventData.Context);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData) => cleanup(eventData.Context);

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        cleanup(eventData.Context);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void SaveChangesCanceled(DbContextEventData eventData) => cleanup(eventData.Context);

    /// <inheritdoc />
    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        cleanup(eventData.Context);
        return Task.CompletedTask;
    }

    private void prepare(DbContext? context)
    {
        if (context is null)
            return;
        cleanup(context);
        var entries = context.ChangeTracker.Entries().ToArray();
        if (entries.Any(entry => (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
            entry.Metadata.FindAnnotation(AuditMetadata.SourceEntity) is not null))
            throw new InvalidOperationException("Hard-delete audit events cannot be written through the ChangeTracker. They are generated only by physical deletions.");

        if (entries.Any(entry => entry.State == EntityState.Deleted &&
            entry.Metadata.FindAnnotation(AuditMetadata.AuditName) is null &&
            (entry.Metadata.FindAnnotation(AuditMetadata.Enabled)?.Value is true ||
             entry.Metadata.ClrType.GetInterfaces().Any(type => type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(IHardDeleteAudited<>)))))
            throw new InvalidOperationException("Hard-delete audit model configuration is missing. Install AddHardDeleteAuditConvention or apply the configurations after OnModelCreating.");

        if (context.ChangeTracker.CascadeDeleteTiming == CascadeTiming.OnSaveChanges ||
            context.ChangeTracker.DeleteOrphansTiming == CascadeTiming.OnSaveChanges)
        {
            if (context.Model.GetEntityTypes().Any(type => type.FindAnnotation(AuditMetadata.AuditName) is not null))
                throw new InvalidOperationException("Hard-delete audit requires immediate cascade and orphan processing (or disabled cascading). Deferred OnSaveChanges processing is not supported.");
        }

        var deletions = entries.Where(entry => entry.State == EntityState.Deleted &&
            entry.Metadata.FindAnnotation(AuditMetadata.AuditName) is not null).ToArray();
        if (deletions.Length == 0)
            return;
        if (!context.Database.IsRelational() || context.Database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
            throw new InvalidOperationException("Hard-delete audit requires relational automatic transactions.");
        if (Transaction.Current is not null || context.Database.GetEnlistedTransaction() is not null)
            throw new InvalidOperationException("Hard-delete audit does not support ambient or enlisted transactions. Use an EF transaction with savepoints.");
        if (context.Database.CurrentTransaction is { } transaction &&
            (!context.Database.AutoSavepointsEnabled || !transaction.SupportsSavepoints))
            throw new InvalidOperationException("Hard-delete audit requires savepoints inside an existing transaction.");

        var pending = _pending.GetOrCreateValue(context);
        var timestamp = DateTimeOffset.UtcNow;
        try
        {
            foreach (var source in deletions)
            {
                var type = (Type)source.Metadata.FindAnnotation(AuditMetadata.UserType)!.Value!;
                if (!_users.TryGetValue(type, out var provider))
                    throw new InvalidOperationException($"No hard-delete audit user provider is registered for '{type}'. Register HardDeleteAuditUserProvider<TUser> as IHardDeleteAuditUserProvider.");
                var user = StampSuppressor.IsSuppressed ? null : provider.GetCurrentUserId();
                if (user is not null && !type.IsInstanceOfType(user))
                    throw new InvalidOperationException($"Audit user provider for '{type}' returned an incompatible actor.");
                var name = (string)source.Metadata.FindAnnotation(AuditMetadata.AuditName)!.Value!;
                var auditType = context.Model.FindEntityType(name)!;
                var sources = (string[])source.Metadata.FindAnnotation(AuditMetadata.SourceMembers)!.Value!;
                var members = (string[])source.Metadata.FindAnnotation(AuditMetadata.KeyMembers)!.Value!;
                var values = new Dictionary<string, object?>
                {
                    ["AuditId"] = Guid.NewGuid(), ["DeletedAt"] = timestamp, ["DeletedBy"] = user
                };
                if (source.Metadata.FindAnnotation(AuditMetadata.KeyType)?.Value is Type keyType)
                {
                    var key = Activator.CreateInstance(keyType)
                        ?? throw new InvalidOperationException($"Cannot create audit key '{keyType}'.");
                    for (var index = 0; index < sources.Length; index++)
                        keyType.GetProperty(members[index])!.SetValue(key, getKeyValue(source, sources[index]));
                    values["EntityKey"] = key;
                }
                else
                {
                    for (var index = 0; index < sources.Length; index++)
                        values[members[index]] = getKeyValue(source, sources[index]);
                }
                var factory = _addMethod.MakeGenericMethod(auditType.ClrType)
                    .CreateDelegate<Func<DbContext, string, Dictionary<string, object?>, EntityEntry>>();
                pending.Add(factory(context, name, values));
            }
        }
        catch
        {
            cleanup(context);
            throw;
        }
    }

    private static object getKeyValue(EntityEntry source, string propertyName)
    {
        var property = source.Property(propertyName);
        if (property.IsTemporary || property.OriginalValue is null)
            throw new InvalidOperationException($"Cannot audit a deletion with a null or temporary key: '{source.Metadata.Name}.{propertyName}'.");
        var value = property.OriginalValue;
        return property.Metadata.GetKeyValueComparer().Snapshot(value)!;
    }

    private static EntityEntry add<TAudit>(DbContext context, string name, Dictionary<string, object?> values) where TAudit : class
    {
        var instance = Activator.CreateInstance<TAudit>();
        if (instance is Dictionary<string, object> dictionary)
        {
            foreach (var pair in values)
                dictionary[pair.Key] = pair.Value!;
        }
        else
        {
            foreach (var pair in values)
                typeof(TAudit).GetProperty(pair.Key)!.SetValue(instance, pair.Value);
        }
        return context.Set<TAudit>(name).Add(instance);
    }

    private void cleanup(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var pending))
            return;
        foreach (var entry in pending)
            entry.State = EntityState.Detached;
        _pending.Remove(context);
    }

}
