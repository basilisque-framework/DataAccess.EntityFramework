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

using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Connection;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Design;

/// <summary>
///     A factory for creating derived <see cref="Microsoft.EntityFrameworkCore.DbContext" /> instances for PostgreSQL. Derive from this class to enable
///     design-time services for context types that do not have a public default constructor. At design-time,
///     derived <see cref="Microsoft.EntityFrameworkCore.DbContext" /> instances can be created in order to enable specific design-time
///     experiences such as Migrations. Design-time services will automatically discover implementations of
///     the underlying <see cref="Microsoft.EntityFrameworkCore.Design.IDesignTimeServices" /> interface that are in the startup assembly or the same assembly as the derived context.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
/// <typeparam name="TDbContext">The type of the context.</typeparam>
public abstract class PostgreSQLBaseDesignTimeDbContextFactory<TDbContext> : BaseDesignTimeDbContextFactory<TDbContext>
    where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <inheritdoc />
    protected sealed override void ConfigureProviderServices(IServiceCollection services)
    {
        services.AddKeyedSingleton<IDbProviderInfo, PostgreSQLDbProviderInfo>(PostgreSQLDbProviderInfo.ProviderKeyName);
        services.AddKeyedSingleton<IDbContextOptionsConfigurator, PostgreSQLDbContextOptionsConfigurator>(PostgreSQLDbProviderInfo.ProviderKeyName);
        services.AddKeyedSingleton<IConnectionStringBuilder, PostgreSQLConnectionStringBuilder>(PostgreSQLDbProviderInfo.ProviderKeyName);
    }
}