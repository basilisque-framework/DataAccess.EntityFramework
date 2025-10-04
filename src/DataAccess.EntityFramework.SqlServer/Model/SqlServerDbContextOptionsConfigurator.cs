using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.SqlServer.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Model;

[RegisterServiceSingleton(As = typeof(IDbContextOptionsConfigurator), ImplementsITypeName = false, Key = SqlServerDbProviderInfo.ProviderKeyName)]
internal class SqlServerDbContextOptionsConfigurator : BaseDbContextOptionsConfigurator
{
    private readonly IConnectionStringBuilder _connectionStringBuilder;

    public SqlServerDbContextOptionsConfigurator(
        [FromKeyedServices(SqlServerDbProviderInfo.ProviderKeyName)] IConnectionStringBuilder connectionStringBuilder
        )
    {
        ArgumentNullException.ThrowIfNull(connectionStringBuilder);

        _connectionStringBuilder = connectionStringBuilder;
    }

    protected override void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _connectionStringBuilder.GetConnectionString(typeof(TDbContext).Name, SqlServerDbProviderInfo.ProviderKeyName);

        optionsBuilder.UseSqlServer(connectionString);
    }
}
