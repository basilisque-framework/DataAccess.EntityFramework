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

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

internal static class StampEntityEntryExtensions
{
    public static bool HasMeaningfulModification(this EntityEntry entry)
    {
        return entry.Properties.Any(property => property.IsModified && property.Metadata.GetStampPropertyKind() is null);
    }

    public static void ProtectCreationProperties(this EntityEntry entry)
    {
        foreach (var property in entry.Properties.Where(isCreationProperty))
            property.IsModified = false;
    }

    private static bool isCreationProperty(PropertyEntry property)
    {
        return property.Metadata.GetStampPropertyKind() is StampPropertyKind.CreatedAt or StampPropertyKind.CreatedBy;
    }
}
