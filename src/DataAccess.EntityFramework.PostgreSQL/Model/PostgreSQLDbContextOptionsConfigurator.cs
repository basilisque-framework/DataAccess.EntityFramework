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
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Model;

[RegisterServiceSingleton(As = typeof(IDbContextOptionsConfigurator), ImplementsITypeName = false, Key = PostgreSQLDbProviderInfo.ProviderKeyName)]
internal class PostgreSQLDbContextOptionsConfigurator : BaseDbContextOptionsConfigurator
{
    private readonly IConnectionStringBuilder _connectionStringBuilder;

    public PostgreSQLDbContextOptionsConfigurator(
        [FromKeyedServices(PostgreSQLDbProviderInfo.ProviderKeyName)] IConnectionStringBuilder connectionStringBuilder
        )
    {
        ArgumentNullException.ThrowIfNull(connectionStringBuilder);

        _connectionStringBuilder = connectionStringBuilder;
    }

    protected override void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _connectionStringBuilder.GetConnectionString(typeof(TDbContext).Name, PostgreSQLDbProviderInfo.ProviderKeyName);

        optionsBuilder.UseNpgsql(connectionString);
    }
}