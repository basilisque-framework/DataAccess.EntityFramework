namespace Basilisque.DataAccess.EntityFramework.Base.Provider;

/// <summary>
/// Represents metadata or configuration information about a database provider.
/// </summary>
public interface IDbProviderInfo
{
    /// <summary>
    /// Gets the unique key identifying the database provider.
    /// </summary>
    string ProviderKey { get; }

    /// <summary>
    /// Gets the name of the database provider.
    /// </summary>
    string ProviderName { get; }
}
