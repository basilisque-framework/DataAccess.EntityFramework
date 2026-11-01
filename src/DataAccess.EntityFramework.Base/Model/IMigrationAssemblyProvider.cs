namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Provides an assembly name for a specific context and database provider.
/// </summary>
[RegisterServiceSingleton(As = typeof(IMigrationAssemblyProvider), ImplementsITypeName = false)]
public interface IMigrationAssemblyProvider
{
    /// <summary>
    /// Returns the matching migration assembly, or null when this provider has no matching mapping.
    /// </summary>
    /// <param name="dbContextType">The concrete context type.</param>
    /// <param name="databaseProvider">The Basilisque database provider key.</param>
    /// <returns>The matching assembly's simple name, or null.</returns>
    string? GetMigrationAssemblyName(Type dbContextType, string databaseProvider);
}
