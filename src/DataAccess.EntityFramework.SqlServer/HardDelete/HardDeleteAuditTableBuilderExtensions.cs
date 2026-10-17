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

using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.HardDelete;

/// <summary>
/// Configures hard-delete auditing alongside SQL Server table configuration.
/// </summary>
public static class HardDeleteAuditTableBuilderExtensions
{
    /// <summary>
    /// Configures SQL Server temporal history and a separate hard-delete audit table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The table builder.</param>
    /// <param name="configureTemporal">Optional temporal history and period configuration.</param>
    /// <param name="configureAudit">Optional audit table configuration.</param>
    /// <returns>The same table builder so configuration can be chained.</returns>
    public static TableBuilder<TEntity> IsTemporalWithHardDeleteAudit<TEntity>(
        this TableBuilder<TEntity> builder,
        Action<TemporalTableBuilder<TEntity>>? configureTemporal = null,
        Action<HardDeleteAuditBuilder<TEntity>>? configureAudit = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (configureTemporal is null)
            builder.IsTemporal();
        else
            builder.IsTemporal(configureTemporal);

        return builder.UseHardDeleteAudit(configureAudit);
    }

    /// <summary>
    /// Configures a separate hard-delete audit table without changing temporal history configuration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The table builder, optionally configured with IsTemporal.</param>
    /// <param name="configure">Optional audit table configuration.</param>
    /// <returns>The same table builder so configuration can be chained.</returns>
    public static TableBuilder<TEntity> UseHardDeleteAudit<TEntity>(
        this TableBuilder<TEntity> builder,
        Action<HardDeleteAuditBuilder<TEntity>>? configure = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        ((IInfrastructure<EntityTypeBuilder<TEntity>>)builder).Instance.UseHardDeleteAudit(configure);

        return builder;
    }
}
