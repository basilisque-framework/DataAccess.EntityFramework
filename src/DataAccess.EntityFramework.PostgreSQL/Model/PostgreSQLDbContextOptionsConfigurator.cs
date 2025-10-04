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