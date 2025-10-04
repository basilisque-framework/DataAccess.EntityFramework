using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Basilisque.DataAccess.EntityFramework.Base.Connection;

/// <inheritdoc />
[RegisterServiceSingleton]
public class ConnectionStringBuilder : IConnectionStringBuilder
{
    private const string ConnectionStringSectionName = "ConnectionString";
    private const string ConnectionStringsSectionName = "ConnectionStrings";

    private readonly IConfiguration _configuration;
    private readonly IDbProviderServiceProvider _dbProviderServiceProvider;

    /// <summary>
    /// Creates a new <see cref="ConnectionStringBuilder"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> that is used to read the connection string from the appsettings.</param>
    /// <param name="dbProviderServiceProvider">The <see cref="IDbProviderServiceProvider"/> that is used to resolve basic configuration necessary for reading the <see cref="IConfiguration"/>.</param>
    public ConnectionStringBuilder(
        IConfiguration configuration,
        IDbProviderServiceProvider dbProviderServiceProvider
        )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dbProviderServiceProvider);

        _configuration = configuration;
        _dbProviderServiceProvider = dbProviderServiceProvider;
    }

    /// <inheritdoc />
    public string GetConnectionString(string? contextName, string? providerKey)
    {
        var dbConfig = _configuration.GetSection(_dbProviderServiceProvider.DatabaseConfigurationSectionName);

        if (!dbConfig.Exists())
            throw new InvalidOperationException($"The configuration section '{_dbProviderServiceProvider.DatabaseConfigurationSectionName}' does not exist.");

        var configSection = findConfigSection(dbConfig, contextName, providerKey);

        var connectionStringInConfig = configSection.Value ?? configSection[ConnectionStringSectionName];

        var connectionStringBuilder = CreateConnectionStringBuilder(connectionStringInConfig);

        if (connectionStringBuilder is null)
            return connectionStringInConfig ?? "";

        readConfigValues(configSection, connectionStringBuilder);

        return connectionStringBuilder.ToString();
    }

    /// <summary>
    /// Creates a new instance of a <see cref="System.Data.Common.DbConnectionStringBuilder"/> based on the given <paramref name="connectionString"/>
    /// </summary>
    /// <param name="connectionString">The connection string used for initializing the connection string builder.</param>
    /// <returns>A <see cref="System.Data.Common.DbConnectionStringBuilder"/> initialized with the provided <paramref name="connectionString"/>, or <see langword="null"/> if no builder could be created.</returns>
    protected virtual System.Data.Common.DbConnectionStringBuilder? CreateConnectionStringBuilder(string? connectionString) => null;

    private IConfigurationSection findConfigSection(IConfigurationSection dbConfig, string? contextName, string? providerKey)
    {
        var result = dbConfig.GetSection(ConnectionStringsSectionName);
        if (!result.Exists())
            return dbConfig;

        result = findSpecificConfigSectionName(result, contextName, providerKey);
        if (result is null)
            return dbConfig;

        return result;
    }

    private IConfigurationSection? findSpecificConfigSectionName(IConfigurationSection connectionStringsSection, string? contextName, string? providerKey)
    {
        IConfigurationSection? contextSection = null;

        if (!string.IsNullOrWhiteSpace(contextName))
        {
            var tmpSection = connectionStringsSection.GetSection(contextName);
            if (tmpSection.Exists())
                contextSection = tmpSection;
        }

        if (!string.IsNullOrWhiteSpace(providerKey))
        {
            var rootSection = contextSection ?? connectionStringsSection;
            var providerSection = rootSection.GetSection(providerKey);
            if (providerSection.Exists())
                return providerSection;
        }

        return contextSection;
    }

    private void readConfigValues(IConfigurationSection? config, System.Data.Common.DbConnectionStringBuilder connectionStringBuilder)
    {
        if (config is null)
            return;

        foreach (var item in config.GetChildren())
        {
            if (item.Key == ConnectionStringSectionName)
                continue;

            connectionStringBuilder[item.Key] = item.Value;
        }
    }
}
