using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Base class that provides common functionality for configuring <see cref="DbContextOptions"/>.
/// </summary>
public abstract class BaseDbContextOptionsConfigurator : IDbContextOptionsConfigurator
{
    /// <inheritdoc />
    public void Configure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        where TDbContext : BaseDbContext<TDbContext>
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        OnConfigure(dbContext, optionsBuilder);
    }

    /// <summary>
    /// Configures the database context by applying additional options to the specified options builder.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context to configure. Must inherit from <see cref="BaseDbContext{TDbContext}"/>.</typeparam>
    /// <param name="dbContext">The database context instance to configure.</param>
    /// <param name="optionsBuilder">The options builder used to configre the database context. Modifications to this builder affect how the context is constructed.</param>
    protected abstract void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder) where TDbContext : BaseDbContext<TDbContext>;
}
