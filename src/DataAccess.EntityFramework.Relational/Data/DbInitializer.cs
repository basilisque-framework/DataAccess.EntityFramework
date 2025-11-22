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

using Basilisque.DataAccess.EntityFramework.Base.Data;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Relational.Data;

/// <summary>
/// Classes that inherit from <see cref="DbInitializer{TDbContext}"/> are being used to execute database initialization and seeding.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public abstract class DbInitializer<TDbContext> : BaseDbInitializer<TDbContext>
    where TDbContext : DbContext, IInitializableDbContext
{
    /// <summary>
    /// Creates a new <see cref="DbInitializer{TDbContext}"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext that should be initialized.</param>
    protected DbInitializer(TDbContext dbContext)
        : base(dbContext)
    { }

    /// <inheritdoc />
    protected override void ExecuteMigration()
    {
        DbContext.Database.Migrate();
    }
}
