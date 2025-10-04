namespace Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;

/// <summary>
/// Defines a service provider for obtaining database provider-specific services.
/// </summary>
public interface IDbProviderServiceProvider
{
    /// <summary>
    /// Gets the name of the configuration section that contains the database settings.
    /// </summary>
    string DatabaseConfigurationSectionName { get; }

    /// <summary>
    /// Get a database provider-specific service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service object to get.</typeparam>
    /// <returns>An instance of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service is not available.</exception>
    T GetRequiredService<T>() where T : notnull;

    /// <summary>
    /// Get a database provider-specific service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service object to get.</typeparam>
    /// <returns>An instance of type <typeparamref name="T"/> if the service is available; otherwise, <see langword="null"/>.</returns>
    T? GetService<T>();
}
