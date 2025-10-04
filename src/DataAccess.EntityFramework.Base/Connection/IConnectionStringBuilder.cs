namespace Basilisque.DataAccess.EntityFramework.Base.Connection;

/// <summary>
/// Provides methods to build connection strings based on configuration settings.
/// </summary>
public interface IConnectionStringBuilder
{
    /// <summary>
    /// Reads connection settings from the configuration file and builds a connection string.
    /// </summary>
    /// <param name="contextName">The name of the DbContext that the current connection string is for.</param>
    /// <param name="providerKey">The key of the database provider.</param>
    /// <returns>The connection string specific for the provided context and provider.</returns>
    string GetConnectionString(string? contextName, string? providerKey);
}