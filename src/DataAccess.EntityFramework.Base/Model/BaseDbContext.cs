using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// A base class with common functionality for DbContexts
/// </summary>
public abstract class BaseDbContext<TDbContext> : DbContext, IInitializableDbContext, IDbContextDesignSupport
    where TDbContext : BaseDbContext<TDbContext>
{
    private static bool _wasAlreadyMigrated = false;
    private readonly IDbProviderServiceProvider _dbProviderServiceProvider;
    private IDbContextOptionsConfigurator? _dbContextOptionsConfigurator = null;

    /// <summary>
    /// Indicates whether the current database context instance is being used in a design-time environment.
    /// </summary>
    public bool IsDesignTime { get; private set; }

    /// <inheritdoc />
    bool IInitializableDbContext.WasAlreadyMigrated { get => _wasAlreadyMigrated; set => _wasAlreadyMigrated = value; }

    /// <inheritdoc />
    bool IDbContextDesignSupport.IsDesignTime { get => IsDesignTime; set => IsDesignTime = value; }

    /// <summary>
    /// Creates a new <see cref="BaseDbContext{TDbContext}"/>.
    /// </summary>
    /// <param name="dbProviderServiceProvider">The <see cref="IDbProviderServiceProvider"/> that is used to resolve database provider specific services.</param>
    protected BaseDbContext(
        IDbProviderServiceProvider dbProviderServiceProvider
    )
    {
        ArgumentNullException.ThrowIfNull(dbProviderServiceProvider);

        _dbProviderServiceProvider = dbProviderServiceProvider;
    }

    /// <summary>
    /// Override this method to configure the database (and other options) to be used for this context.
    /// This method is called for each instance of the context that is created. The base implementation calls <see cref="IDbContextOptionsConfigurator.Configure{TDbContext}(BaseDbContext{TDbContext}, DbContextOptionsBuilder)"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In situations where an instance of <see cref="DbContextOptions" /> may or may not have been passed to
    ///         the constructor, you can use <see cref="DbContextOptionsBuilder.IsConfigured" /> to determine if the
    ///         options have already been set, and skip some or all of the logic in <see cref="OnConfiguring(DbContextOptionsBuilder)" />.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see>
    ///         for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="optionsBuilder">A builder used to create or modify options for this context. Databases (and other options) typically define extension methods on this object that allow you to configure the context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        _dbContextOptionsConfigurator ??= _dbProviderServiceProvider.GetRequiredService<IDbContextOptionsConfigurator>();

        _dbContextOptionsConfigurator.Configure(this, optionsBuilder);
    }
}