using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Basilisque.DataAccess.EntityFramework.SQLite.Connection;

/// <inheritdoc />
[RegisterServiceSingleton(As = typeof(IConnectionStringBuilder), Key = SQLiteDbProviderInfo.ProviderKeyName)]
internal class SQLiteConnectionStringBuilder : ConnectionStringBuilder
{
    public SQLiteConnectionStringBuilder(
        IConfiguration configuration,
        IDbProviderServiceProvider dbProviderServiceProvider
        )
        : base(configuration, dbProviderServiceProvider)
    { }

    protected override DbConnectionStringBuilder? CreateConnectionStringBuilder(string? connectionString)
    {
        return new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    }
}