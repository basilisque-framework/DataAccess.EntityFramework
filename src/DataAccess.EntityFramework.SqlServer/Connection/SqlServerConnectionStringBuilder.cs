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
