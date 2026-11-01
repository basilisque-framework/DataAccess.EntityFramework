using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;

/// <summary>Configures explicit context/provider migration assembly selections.</summary>
public static class MigrationAssemblyServiceCollectionExtensions
{
    /// <summary>Overrides generated mappings for the specified context/provider pair.</summary>
    /// <typeparam name="TDbContext">The context whose migrations are selected.</typeparam>
    /// <param name="services">The application services.</param>
    /// <param name="provider">The Basilisque database provider key.</param>
    /// <param name="assembly">The assembly containing the migrations.</param>
    /// <returns>The application services.</returns>
    public static IServiceCollection AddMigrationAssembly<TDbContext>(this IServiceCollection services, string provider, Assembly assembly)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentNullException.ThrowIfNull(assembly);
        services.AddSingleton(new MigrationAssemblyOverride(typeof(TDbContext), provider,
            assembly.GetName().Name ?? throw new ArgumentException("The assembly must have a simple name.", nameof(assembly))));
        services.TryAddSingleton<IMigrationAssemblyResolver, MigrationAssemblyResolver>();
        return services;
    }
}
