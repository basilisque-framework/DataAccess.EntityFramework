using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Provides functionality to configure <see cref="DbContextOptions"/>
/// </summary>
public interface IDbContextOptionsConfigurator
{
    /// <summary>
    /// Configures the provided <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context being configured.</typeparam>
    /// <param name="dbContext">The database context being configured.</param>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    void Configure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        where TDbContext : BaseDbContext<TDbContext>;
}
