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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

internal static class SoftDeleteProcessor
{
    public static void Apply(DbContext context, DateTimeOffset timestamp)
    {
        var entries = context.ChangeTracker.Entries().ToArray();
        var softDeletes = SoftDeleteSuppressor.IsHardDeleteAllowed
            ? []
            : entries.Where(entry => entry.State == EntityState.Deleted && entry.FindSoftDeleteTimestamp() is not null).ToArray();

        if (softDeletes.Length > 0)
            convertDeletes(context, entries, softDeletes, timestamp);

        foreach (var entry in entries.Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.FindSoftDeleteTimestamp() is { CurrentValue: null })
            {
                foreach (var user in entry.Properties.Where(property => property.Metadata.GetStampPropertyKind() == StampPropertyKind.DeletedBy))
                    user.CurrentValue = null;
            }
        }
    }

    private static void convertDeletes(DbContext context, EntityEntry[] entries, EntityEntry[] softDeletes, DateTimeOffset timestamp)
    {
        var protectedEntries = new HashSet<EntityEntry>(softDeletes);
        var ownedDeletes = new HashSet<EntityEntry>();
        bool foundOwned;
        do
        {
            foundOwned = false;
            foreach (var entry in entries.Where(entry => entry.State == EntityState.Deleted && entry.Metadata.IsOwned()))
            {
                if (ownedDeletes.Contains(entry))
                    continue;

                var ownership = entry.Metadata.FindOwnership();
                if (ownership is not null && protectedEntries.Any(principal => references(entry, ownership, principal, originalValues: false)))
                {
                    ownedDeletes.Add(entry);
                    protectedEntries.Add(entry);
                    foundOwned = true;
                }
            }
        } while (foundOwned);

        foreach (var entry in entries.Except(ownedDeletes))
        {
            foreach (var foreignKey in entry.Metadata.GetForeignKeys())
            {
                if (!softDeletes.Any(principal => references(entry, foreignKey, principal, originalValues: true) ||
                                                  references(entry, foreignKey, principal, originalValues: false)))
                    continue;

                var wouldCascade = context.ChangeTracker.CascadeDeleteTiming != CascadeTiming.Never &&
                    foreignKey.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.ClientCascade or DeleteBehavior.SetNull or DeleteBehavior.ClientSetNull;

                if (wouldCascade || entry.State == EntityState.Deleted ||
                    foreignKey.Properties.Any(property => entry.Property(property.Name).IsModified))
                    throw new InvalidOperationException($"Soft deletion of a principal would delete or change dependent entity '{entry.Metadata.Name}'. No automatic soft-delete cascade is supported. Configure the relationship with DeleteBehavior.ClientNoAction and handle dependent changes explicitly.");
            }
        }

        foreach (var entry in ownedDeletes)
            entry.State = EntityState.Unchanged;

        foreach (var entry in softDeletes)
        {
            var date = entry.FindSoftDeleteTimestamp()!;
            var previousDate = date.OriginalValue;
            entry.State = EntityState.Unchanged;
            date.CurrentValue = previousDate ?? timestamp;
            date.IsModified = previousDate is null;
        }

    }

    private static bool references(EntityEntry dependent, IForeignKey foreignKey, EntityEntry principal, bool originalValues)
    {
        if (!foreignKey.PrincipalEntityType.IsAssignableFrom(principal.Metadata))
            return false;

        for (var index = 0; index < foreignKey.Properties.Count; index++)
        {
            var property = dependent.Property(foreignKey.Properties[index].Name);
            var foreignKeyValue = originalValues ? property.OriginalValue : property.CurrentValue;
            var principalKeyValue = principal.Property(foreignKey.PrincipalKey.Properties[index].Name).CurrentValue;
            if (foreignKeyValue is null || !foreignKey.Properties[index].GetKeyValueComparer().Equals(foreignKeyValue, principalKeyValue))
                return false;
        }

        return true;
    }
}
