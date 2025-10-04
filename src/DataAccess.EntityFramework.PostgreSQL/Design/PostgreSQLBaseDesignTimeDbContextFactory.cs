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