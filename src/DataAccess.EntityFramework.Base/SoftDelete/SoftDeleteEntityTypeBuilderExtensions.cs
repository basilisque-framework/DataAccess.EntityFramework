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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

/// <summary>
/// Configures soft deletion using shadow properties.
/// </summary>
public static class SoftDeleteEntityTypeBuilderExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        /// <summary>
        /// Adds shadow deletion properties with nullable Guid user keys and a query filter.
        /// </summary>
        public EntityTypeBuilder<TEntity> UseShadowSoftDelete()
        {
            return builder.UseShadowSoftDelete<TEntity, Guid>();
        }

        /// <summary>
        /// Adds shadow deletion properties with nullable value-type user keys and a query filter.
        /// </summary>
        /// <typeparam name="TUserKey">The deletion user-key type.</typeparam>
        public EntityTypeBuilder<TEntity> UseShadowSoftDelete<TUserKey>()
            where TUserKey : struct
        {
            builder.UseShadowSoftDeleteTimestamp();
            SoftDeleteModelBuilderExtensions.configureUserProperty(builder.Property<TUserKey?>(nameof(ISoftDelete.DeletedBy)).Metadata);
            return builder;
        }

        /// <summary>
        /// Adds shadow deletion properties with nullable reference-type user keys and a query filter.
        /// </summary>
        /// <typeparam name="TUserKey">The deletion user-key type.</typeparam>
        public EntityTypeBuilder<TEntity> UseShadowSoftDeleteOfRef<TUserKey>()
            where TUserKey : class
        {
            builder.UseShadowSoftDeleteTimestamp();
            SoftDeleteModelBuilderExtensions.configureUserProperty(builder.Property<TUserKey>(nameof(ISoftDelete.DeletedBy)).Metadata);
            return builder;
        }

        /// <summary>
        /// Adds only a nullable shadow deletion time and query filter.
        /// </summary>
        public EntityTypeBuilder<TEntity> UseShadowSoftDeleteTimestamp()
        {
            ArgumentNullException.ThrowIfNull(builder);
            SoftDeleteModelBuilderExtensions.configureTimestampAndFilter(builder);
            return builder;
        }
    }
}
