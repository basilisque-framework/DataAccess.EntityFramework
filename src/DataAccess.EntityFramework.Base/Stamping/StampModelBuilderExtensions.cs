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
using Microsoft.EntityFrameworkCore.Metadata;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Provides model configuration for interface-backed entity stamps.
/// </summary>
public static class StampModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        /// Configures stamp properties for every entity type that implements a stamp interface.
        /// </summary>
        public void ApplyStampConfigurations()
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                configureDateProperties(modelBuilder, clrType);
                configureUserProperties(modelBuilder, clrType);
            }
        }
    }

    private static void configureDateProperties(ModelBuilder modelBuilder, Type clrType)
    {
        if (typeof(IStampCreateDate).IsAssignableFrom(clrType))
            configureProperty(modelBuilder, clrType, nameof(IStampCreateDate.CreatedAt), StampPropertyKind.CreatedAt);

        if (typeof(IStampModifyDate).IsAssignableFrom(clrType))
            configureProperty(modelBuilder, clrType, nameof(IStampModifyDate.ModifiedAt), StampPropertyKind.ModifiedAt);
    }

    private static void configureUserProperties(ModelBuilder modelBuilder, Type clrType)
    {
        if (implementsGenericInterface(clrType, typeof(IStampCreateUser<>)))
            configureProperty(modelBuilder, clrType, nameof(IStampCreateUser<Guid>.CreatedBy), StampPropertyKind.CreatedBy);

        if (implementsGenericInterface(clrType, typeof(IStampModifyUser<>)))
            configureProperty(modelBuilder, clrType, nameof(IStampModifyUser<Guid>.ModifiedBy), StampPropertyKind.ModifiedBy);
    }

    private static void configureProperty(ModelBuilder modelBuilder, Type clrType, string propertyName, StampPropertyKind propertyKind)
    {
        var property = modelBuilder.Entity(clrType).Property(propertyName).Metadata;
        property.SetStampPropertyKind(propertyKind);

        if (propertyKind is StampPropertyKind.CreatedAt or StampPropertyKind.CreatedBy)
            property.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        if (property.ClrType == typeof(string))
            property.SetMaxLength(256);
    }

    private static bool implementsGenericInterface(Type clrType, Type genericInterfaceType)
    {
        return clrType.GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterfaceType);
    }
}
