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
/// Provides functionality to configure <see cref="DbContextOptions"/>
/// </summary>
public interface IDbContextOptionsConfigurator
{
    /// <summary>
    /// Configures the provided <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context being configured.</typeparam>
    /// <param name="dbContext">The database context being configured.</param>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    void Configure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        where TDbContext : BaseDbContext<TDbContext>;
}
