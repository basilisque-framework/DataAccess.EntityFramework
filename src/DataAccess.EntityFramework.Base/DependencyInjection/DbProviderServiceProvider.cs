using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;

/// <inheritdoc />
[RegisterServiceSingleton]
public class DbProviderServiceProvider : IDbProviderServiceProvider
{
    private const string DatabaseProviderConfigurationKey = "Provider";

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public string DatabaseConfigurationSectionName => GetDatabaseConfigurationSectionName();

    /// <summary>
    /// Creates a new <see cref="DbProviderServiceProvider"/>
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> that will be used to resolve the configured database provider.</param>
    /// <param name="serviceProvider">The service provider that will be used to resolve services.</param>
    public DbProviderServiceProvider(
        IConfiguration configuration,
        IServiceProvider serviceProvider
        )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public T GetRequiredService<T>()
        where T : notnull
    {
        if (getDbProviderSpecificService<T>(out var result))
            return result;

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc />
    public T? GetService<T>()
    {
        if (getDbProviderSpecificService<T>(out var result))
            return result;

        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Returns the name of the configuration section that contains the database settings.
    /// </summary>
    /// <returns>A string representing the name of the database configuration section.</returns>
    protected virtual string GetDatabaseConfigurationSectionName()
    {
        return "Database";
    }

    /// <summary>
    /// Returns the name of the default database provider used by the implementation when no provider is specified in the <see cref="IConfiguration"/>.
    /// </summary>
    /// <remarks>Override this method in a derived class to specify a different default provider name if required by your applications's data access strategy.</remarks>
    /// <returns>A string containing the name of the default provider or <see langword="null"/>. The default implementation returns <see langword="null"/>.</returns>
    protected virtual string? GetDefaultDatabaseProviderName() => null;

    /// <summary>
    /// Retrieves the name of the configured database provider from the application's configuration section.
    /// </summary>
    /// <remarks>
    /// This method first attempts to obtain the provider name from the configuration section defined by the implementation.
    /// If the provider name is not set, it falls back to a default provider name. Override this method to customize how the provider name is determined.
    /// </remarks>
    /// <returns>A string containing the name of the database provider as specified in the configuration. If no provider is configured, an exception is thrown.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no database provider is specified in the configuration and no default provider name is available.</exception>"
    protected virtual string GetDatabaseProviderName()
    {
        var databaseSection = _configuration.GetSection(GetDatabaseConfigurationSectionName());

        var providerName = databaseSection[DatabaseProviderConfigurationKey];

        if (!string.IsNullOrWhiteSpace(providerName))
            return providerName;

        providerName = GetDefaultDatabaseProviderName();

        if (!string.IsNullOrWhiteSpace(providerName))
            return providerName;

        throw new InvalidOperationException($"No database provider is configured. Please specify a provider in the '{GetDatabaseConfigurationSectionName()}:{DatabaseProviderConfigurationKey}' configuration section.");
    }

    private bool getDbProviderSpecificService<T>([NotNullWhen(true)] out T? value)
    {
        var providerName = GetDatabaseProviderName();

        value = _serviceProvider.GetKeyedService<T>(providerName);

        return value is not null;
    }
}
