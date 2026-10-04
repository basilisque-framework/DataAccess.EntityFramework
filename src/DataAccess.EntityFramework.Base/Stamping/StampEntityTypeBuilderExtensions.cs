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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Provides configuration for shadow-property entity stamps.
/// </summary>
public static class StampEntityTypeBuilderExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        /// <summary>
        /// Adds shadow properties for the creation timestamp and creator key.
        /// </summary>
        /// <typeparam name="TUserKey">The type of the user key.</typeparam>
        /// <returns>The entity type builder.</returns>
        public EntityTypeBuilder<TEntity> UseShadowCreationStamp<TUserKey>()
        {
            ArgumentNullException.ThrowIfNull(builder);

            configureProperty(builder.Property<DateTimeOffset>(nameof(IStampCreateDate.CreatedAt)).Metadata, StampPropertyKind.CreatedAt);
            configureProperty(builder.Property<TUserKey>(nameof(IStampCreateUser<TUserKey>.CreatedBy)).Metadata, StampPropertyKind.CreatedBy);

            return builder;
        }

        /// <summary>
        /// Adds shadow properties for the modification timestamp and modifier key.
        /// </summary>
        /// <typeparam name="TUserKey">The type of the user key.</typeparam>
        /// <returns>The entity type builder.</returns>
        public EntityTypeBuilder<TEntity> UseShadowModificationStamp<TUserKey>()
        {
            ArgumentNullException.ThrowIfNull(builder);

            configureProperty(builder.Property<DateTimeOffset>(nameof(IStampModifyDate.ModifiedAt)).Metadata, StampPropertyKind.ModifiedAt);
            configureProperty(builder.Property<TUserKey>(nameof(IStampModifyUser<TUserKey>.ModifiedBy)).Metadata, StampPropertyKind.ModifiedBy);

            return builder;
        }
    }

    private static void configureProperty(IMutableProperty property, StampPropertyKind propertyKind)
    {
        property.SetStampPropertyKind(propertyKind);

        if (propertyKind is StampPropertyKind.CreatedAt or StampPropertyKind.CreatedBy)
            property.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        if (property.ClrType == typeof(string))
            property.SetMaxLength(256);
    }
}
