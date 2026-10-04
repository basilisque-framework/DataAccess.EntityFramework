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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Applies creation and modification timestamps to configured entities.
/// </summary>
public sealed class TimestampStampHandler : IStampHandler
{
    /// <inheritdoc />
    public void UpdateStampProperties(DbContext context, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entry in context.ChangeTracker.Entries())
            updateEntry(entry, timestamp);
    }

    private static void updateEntry(EntityEntry entry, DateTimeOffset timestamp)
    {
        if (entry.State == EntityState.Added)
        {
            updateAddedEntry(entry, timestamp);
        }

        if (entry.State == EntityState.Modified)
            updateModifiedEntry(entry, timestamp);
    }

    private static void updateAddedEntry(EntityEntry entry, DateTimeOffset timestamp)
    {
        setTimestamp(entry, StampPropertyKind.CreatedAt, timestamp);
        setTimestamp(entry, StampPropertyKind.ModifiedAt, timestamp);
    }

    private static void updateModifiedEntry(EntityEntry entry, DateTimeOffset timestamp)
    {
        entry.ProtectCreationProperties();

        if (entry.HasMeaningfulModification())
            setTimestamp(entry, StampPropertyKind.ModifiedAt, timestamp);
    }

    private static void setTimestamp(EntityEntry entry, StampPropertyKind propertyKind, DateTimeOffset timestamp)
    {
        foreach (var property in entry.Properties.Where(property => property.Metadata.GetStampPropertyKind() == propertyKind))
            property.CurrentValue = timestamp;
    }
}
