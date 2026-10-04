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

using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

/// <summary>
/// Configures CLR-backed soft deletion and named query filters.
/// </summary>
public static class SoftDeleteModelBuilderExtensions
{
    private const string PreservedApplicationFilterName = "Basilisque:ApplicationQueryFilter";

    /// <summary>
    /// The name used to selectively disable the soft-delete query filter.
    /// </summary>
    public const string QueryFilterName = "Basilisque:SoftDelete";

    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        /// Configures soft-deletable entities. Call after application model configuration.
        /// </summary>
        public void ApplySoftDeleteConfigurations()
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            applyConfigurations(modelBuilder.Model);
        }
    }

    internal static void applyConfigurations(IMutableModel model)
    {
        foreach (var entityType in model.GetEntityTypes().OrderBy(type => type.BaseType is not null).ToArray())
        {
            var supportsSoftDelete = typeof(ISoftDeleteTimestamp).IsAssignableFrom(entityType.ClrType);
            var date = entityType.FindProperty(nameof(ISoftDeleteTimestamp.DeletedAt));
            var hasConfiguredTimestamp = date?.IsSoftDeleteTimestamp() is true;

            if (!supportsSoftDelete && !hasConfiguredTimestamp)
                continue;

            if (entityType.BaseType is not null)
            {
                if (entityType.GetRootType().FindProperty(nameof(ISoftDeleteTimestamp.DeletedAt))?.IsSoftDeleteTimestamp() is not true)
                    throw new InvalidOperationException($"Soft deletion must be configured on the root entity of the hierarchy containing '{entityType.Name}'.");

                configureAuditProperties(entityType);
                continue;
            }

            configureTimestampAndFilter(entityType);
            configureAuditProperties(entityType);
        }
    }

    private static void configureAuditProperties(IMutableEntityType entityType)
    {
        if (!entityType.ClrType.GetInterfaces().Any(type => type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(ISoftDelete<>) || type.GetGenericTypeDefinition() == typeof(ISoftDeleteOfRef<>))))
            return;

        configureUserProperty(getMappedProperty(entityType, nameof(ISoftDelete.DeletedBy)));
    }

    internal static void configureTimestampAndFilter(EntityTypeBuilder builder)
    {
        builder.Property<DateTimeOffset?>(nameof(ISoftDeleteTimestamp.DeletedAt));
        configureTimestampAndFilter(builder.Metadata);
    }

    private static void configureTimestampAndFilter(IMutableEntityType entityType)
    {
        if (entityType.IsOwned() || entityType.BaseType is not null)
            throw new InvalidOperationException($"Soft deletion can only be configured on non-owned root entities, not '{entityType.Name}'. Configure the owning or root entity instead.");

        var date = getMappedProperty(entityType, nameof(ISoftDeleteTimestamp.DeletedAt));
        if (date.ClrType != typeof(DateTimeOffset?))
            throw new InvalidOperationException($"Soft-delete property '{entityType.Name}.{date.Name}' must have type DateTimeOffset?.");

        if (entityType.FindProperty(nameof(ISoftDeleteTimestamp.IsDeleted)) is not null)
            throw new InvalidOperationException($"Soft-delete property '{entityType.Name}.IsDeleted' is computed and must not be mapped. Ignore it in the EF model.");

        date.IsNullable = true;
        date.SetSoftDeleteTimestamp();
        var parameter = Expression.Parameter(entityType.ClrType, "entity");
        Expression<Func<object, DateTimeOffset?>> dateAccess = entity => EF.Property<DateTimeOffset?>(entity, nameof(ISoftDeleteTimestamp.DeletedAt));
        var method = ((MethodCallExpression)dateAccess.Body).Method;
        var filter = Expression.Lambda(Expression.Equal(Expression.Call(method, parameter, Expression.Constant(date.Name)),
            Expression.Constant(null, typeof(DateTimeOffset?))), parameter);
        preserveAnonymousFilter(entityType);
        entityType.SetQueryFilter(QueryFilterName, filter);
    }

    private static void preserveAnonymousFilter(IMutableEntityType entityType)
    {
        var filters = entityType.GetDeclaredQueryFilters().ToArray();
        var anonymous = filters.SingleOrDefault(filter => filter.IsAnonymous);
        if (anonymous is null)
            return;

        if (filters.Any(filter => filter.Key == PreservedApplicationFilterName))
            throw new InvalidOperationException($"Query filter name '{PreservedApplicationFilterName}' is reserved for preserving anonymous filters on '{entityType.Name}'.");

        // EF does not allow anonymous and named filters to coexist.
        entityType.SetQueryFilter((LambdaExpression?)null);
        foreach (var existing in filters)
            entityType.SetQueryFilter(existing.IsAnonymous ? PreservedApplicationFilterName : existing.Key, existing.Expression);
    }

    internal static void configureUserProperty(IMutableProperty property)
    {
        if (property.ClrType.IsValueType && Nullable.GetUnderlyingType(property.ClrType) is null)
            throw new InvalidOperationException($"Soft-delete property '{property.DeclaringType.Name}.{property.Name}' must have a nullable user-key type.");

        property.IsNullable = true;
        property.SetStampPropertyKind(StampPropertyKind.DeletedBy);

        if (property.ClrType == typeof(string))
            property.SetMaxLength(256);
    }

    private static IMutableProperty getMappedProperty(IMutableEntityType entityType, string propertyName)
    {
        return entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Soft-delete property '{entityType.Name}.{propertyName}' must be mapped in the EF model.");
    }
}
