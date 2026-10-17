using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.DataAccess.EntityFramework.Relational.Design;

/// <summary>
/// Provides common relational design-time services for standalone and child factories.
/// </summary>
/// <typeparam name="TDbContext">The type of the context.</typeparam>
public class RelationalBaseDesignTimeDbContextFactory<TDbContext> : BaseDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public override void ConfigureProviderServices(IServiceCollection services, string[] args, bool isChildFactory)
    {
        ArgumentNullException.ThrowIfNull(services);

        base.ConfigureProviderServices(services, args, isChildFactory);

        services.TryAddScoped<HardDeleteAuditInterceptor>();
    }
}
