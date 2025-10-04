using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Connection;

/// <inheritdoc />
[RegisterServiceSingleton(As = typeof(IConnectionStringBuilder), Key = PostgreSQLDbProviderInfo.ProviderKeyName)]
internal class PostgreSQLConnectionStringBuilder : ConnectionStringBuilder
{
    public PostgreSQLConnectionStringBuilder(
        IConfiguration configuration,
        IDbProviderServiceProvider dbProviderServiceProvider
        )
        : base(configuration, dbProviderServiceProvider)
    { }

    protected override DbConnectionStringBuilder? CreateConnectionStringBuilder(string? connectionString)
    {
        return new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    }
}