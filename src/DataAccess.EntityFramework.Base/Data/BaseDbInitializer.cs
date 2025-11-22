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

using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Data;

/// <summary>
/// Classes that inherit from <see cref="BaseDbInitializer{TDbContext}"/> are being used to execute database initialization and seeding.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public abstract class BaseDbInitializer<TDbContext> : IDbInitializer
    where TDbContext : DbContext, IInitializableDbContext
{
    /// <summary>
    /// The DbContext that should be initialized.
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <summary>
    /// Creates a new <see cref="BaseDbInitializer{TDbContext}"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext that should be initialized.</param>
    protected BaseDbInitializer(TDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        DbContext = dbContext;
    }

    /// <inheritdoc />
    public virtual void BeforeMigration()
    { /* for overriding purposes only */ }

    /// <inheritdoc />
    public virtual void AfterMigration()
    { /* for overriding purposes only */ }

    /// <inheritdoc />
    public void MigrateDatabase()
    {
        if (DbContext.WasAlreadyMigrated)
            return;

        ExecuteMigration();

        DbContext.WasAlreadyMigrated = true;
    }

    /// <inheritdoc />
    public virtual void SeedData()
    { /* for overriding purposes only */ }

    /// <summary>
    /// Executes the database migration.
    /// </summary>
    protected abstract void ExecuteMigration();
}
