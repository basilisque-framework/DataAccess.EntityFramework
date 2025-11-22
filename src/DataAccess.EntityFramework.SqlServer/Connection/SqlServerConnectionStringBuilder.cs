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
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Connection;

/// <inheritdoc />
[RegisterServiceSingleton(As = typeof(IConnectionStringBuilder), Key = SqlServerDbProviderInfo.ProviderKeyName)]
internal class SqlServerConnectionStringBuilder : ConnectionStringBuilder
{
    public SqlServerConnectionStringBuilder(
        IConfiguration configuration,
        IDbProviderServiceProvider dbProviderServiceProvider
        )
        : base(configuration, dbProviderServiceProvider)
    { }

    protected override DbConnectionStringBuilder? CreateConnectionStringBuilder(string? connectionString)
    {
        return new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
    }
}
