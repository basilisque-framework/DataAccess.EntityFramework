/*
   Copyright 2025 Alexander Stärk

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

namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Base class that provides common functionality for configuring <see cref="DbContextOptions"/>.
/// </summary>
public abstract class BaseDbContextOptionsConfigurator : IDbContextOptionsConfigurator
{
    /// <inheritdoc />
    public void Configure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        where TDbContext : BaseDbContext<TDbContext>
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        OnConfigure(dbContext, optionsBuilder);
    }

    /// <summary>
    /// Configures the database context by applying additional options to the specified options builder.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context to configure. Must inherit from <see cref="BaseDbContext{TDbContext}"/>.</typeparam>
    /// <param name="dbContext">The database context instance to configure.</param>
    /// <param name="optionsBuilder">The options builder used to configre the database context. Modifications to this builder affect how the context is constructed.</param>
    protected abstract void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder) where TDbContext : BaseDbContext<TDbContext>;
}
