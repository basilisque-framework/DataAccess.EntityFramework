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

using Microsoft.EntityFrameworkCore.Metadata;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

internal static class StampPropertyExtensions
{
    private const string PropertyKindAnnotationName = "Basilisque:StampPropertyKind";

    public static StampPropertyKind? GetStampPropertyKind(this IReadOnlyProperty property)
    {
        return property.FindAnnotation(PropertyKindAnnotationName)?.Value as StampPropertyKind?;
    }

    public static void SetStampPropertyKind(this IMutableProperty property, StampPropertyKind propertyKind)
    {
        property.SetAnnotation(PropertyKindAnnotationName, propertyKind);
    }
}
