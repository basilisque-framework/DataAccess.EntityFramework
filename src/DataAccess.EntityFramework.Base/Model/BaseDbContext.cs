using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

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
    private IDbProviderInfo? _dbProviderInfo = null;

    /// <summary>
    /// Gets the file path to the source file where the <typeparamref name="TDbContext"/> is defined.
    /// </summary>
    protected internal string DbContextSourceFilePath { get; }

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
    /// <param name="dbContextSourceFilePath">The source file path where the <typeparamref name="TDbContext"/> is defined. This parameter is automatically populated by the compiler.</param>
    protected BaseDbContext(
        IDbProviderServiceProvider dbProviderServiceProvider,
        [CallerFilePath] string dbContextSourceFilePath = ""
    )
    {
        ArgumentNullException.ThrowIfNull(dbProviderServiceProvider);

        _dbProviderServiceProvider = dbProviderServiceProvider;

        DbContextSourceFilePath = dbContextSourceFilePath;
    }

    /// <summary>
    /// Retrieves a collection of database script names to be executed during initialization or migration.
    /// </summary>
    /// <returns>An enumerable collection of strings, each representing the name of a database script. The collection may be empty if no scripts are provided.</returns>
    public IEnumerable<string> GetDbScripts()
    {
        var provider = GetProviderDirectoryName();

        return GetDbScripts(provider);
    }

    /// <summary>
    /// Retrieves the directory name corresponding to the current database provider.
    /// </summary>
    /// <returns>A string representing the directory name for the database provider, or <see langword="null"/> if the provider name is <see langword="null"/>.</returns>
    protected internal string? GetProviderDirectoryName()
    {
        _dbProviderInfo ??= _dbProviderServiceProvider.GetRequiredService<IDbProviderInfo>();

        var providerDirectoryName = toValidDirectoryName(_dbProviderInfo.ProviderKey);

        return providerDirectoryName;
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

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        if (IsDesignTime)
            ConfigureDesignTimeConventions(configurationBuilder);
    }

    /// <summary>
    ///     Override this method to set defaults and configure conventions for design time before they run. This method is invoked before <see cref="DbContext.OnModelCreating" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context (via <see cref="DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
    ///         then this method will not be run. However, it will still run when creating a compiled model.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-pre-convention">Pre-convention model building in EF Core</see> for more information and
    ///         examples.
    ///     </para>
    /// </remarks>
    /// <param name="configurationBuilder">
    ///     The builder being used to set defaults and configure conventions that will be used to build the model for this context.
    /// </param>
    protected virtual void ConfigureDesignTimeConventions(ModelConfigurationBuilder configurationBuilder)
    {
        AddDesignTimeModelDbScriptsConvention(configurationBuilder);
    }

    /// <summary>
    /// Adds the <see cref="DbScripts.ModelDbScriptsConvention{TDbContext}"/> to the list of conventions
    /// </summary>
    /// <param name="configurationBuilder">The builder being used to set defaults and configure conventions that will be used to build the model for this context.</param>
    protected virtual void AddDesignTimeModelDbScriptsConvention(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(sp => new DbScripts.ModelDbScriptsConvention<TDbContext>(this));
    }

    /// <summary>
    /// Retrieves a collection of database script statement names to be executed during initialization or migration.
    /// </summary>
    /// <remarks>Override this method in a derived class to provide custom database scripts. The default implementation returns an empty collection.</remarks>
    /// <param name="providerDirectoryName">The scripts subdirectory name corresponding to the current database provider.</param>
    /// <returns>An enumerable collection of strings, each representing a name of a database script. The collection may be empty if no scripts are provided.</returns>
    protected virtual IEnumerable<string> GetDbScripts(string? providerDirectoryName)
    { /* for overriding purposes only */ yield break; }

    private string toValidDirectoryName(string? name, char replacementChar = '_')
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The directory name cannot be null or empty.", nameof(name));

        var invalidChars = System.IO.Path.GetInvalidPathChars();

        var cleaned = new string(name
            .Select(ch => invalidChars.Contains(ch) ? replacementChar : ch)
            .ToArray());

        cleaned = cleaned.Trim(' ', '.');

        return string.IsNullOrWhiteSpace(cleaned) ? $"{replacementChar}" : cleaned;
    }
}