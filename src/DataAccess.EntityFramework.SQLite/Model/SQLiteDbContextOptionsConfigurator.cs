using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.SQLite.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.SQLite.Model;

[RegisterServiceSingleton(As = typeof(IDbContextOptionsConfigurator), ImplementsITypeName = false, Key = SQLiteDbProviderInfo.ProviderKeyName)]
internal class SQLiteDbContextOptionsConfigurator : BaseDbContextOptionsConfigurator
{
    private readonly IConnectionStringBuilder _connectionStringBuilder;

    public SQLiteDbContextOptionsConfigurator(
        [FromKeyedServices(SQLiteDbProviderInfo.ProviderKeyName)] IConnectionStringBuilder connectionStringBuilder
        )
    {
        ArgumentNullException.ThrowIfNull(connectionStringBuilder);

        _connectionStringBuilder = connectionStringBuilder;
    }

    protected override void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _connectionStringBuilder.GetConnectionString(typeof(TDbContext).Name, SQLiteDbProviderInfo.ProviderKeyName);

        optionsBuilder.UseSqlite(connectionString);
    }
}