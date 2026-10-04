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
using Microsoft.EntityFrameworkCore.Metadata;

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

internal static class SoftDeletePropertyExtensions
{
    private const string TimestampAnnotationName = "Basilisque:SoftDeleteTimestamp";

    public static void SetSoftDeleteTimestamp(this IMutableProperty property)
    {
        property.SetAnnotation(TimestampAnnotationName, true);
    }

    public static PropertyEntry? FindSoftDeleteTimestamp(this EntityEntry entry)
    {
        var property = entry.Metadata.FindProperty(nameof(ISoftDeleteTimestamp.DeletedAt));
        return property?.IsSoftDeleteTimestamp() is true ? entry.Property(property.Name) : null;
    }

    public static bool IsSoftDeleteTimestamp(this IReadOnlyProperty property)
    {
        return property.FindAnnotation(TimestampAnnotationName)?.Value is true;
    }

    public static bool IsSoftDeletion(this EntityEntry entry)
    {
        var date = entry.FindSoftDeleteTimestamp();
        return date?.CurrentValue is not null &&
            (entry.State == EntityState.Added ||
             (entry.State == EntityState.Modified && date.IsModified && date.OriginalValue is null));
    }
}
