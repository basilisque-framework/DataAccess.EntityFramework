namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Defines a contract for providing information about the migration assembly used in database migrations.
/// </summary>
[RegisterServiceSingleton(As = typeof(IMigrationAssemblyProvider), ImplementsITypeName = false)]
public interface IMigrationAssemblyProvider
{
    /// <summary>
    /// Retrieves the simple name of the assembly containing the current migration, if available.
    /// </summary>
    /// <returns>The simple name of the migration assembly, or <see langword="null"/> for the default Entity Framework behaviour.</returns>
    string? GetMigrationAssemblyName();
}
