namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>Resolves migration assemblies without depending on registration order.</summary>
[RegisterServiceSingleton(As = typeof(IMigrationAssemblyResolver), ImplementsITypeName = false)]
public interface IMigrationAssemblyResolver
{
    /// <summary>Resolves an explicit override or an unambiguous generated mapping.</summary>
    /// <param name="dbContextType">The concrete context type.</param>
    /// <param name="databaseProvider">The Basilisque database provider key.</param>
    /// <returns>The assembly name, or null to use Entity Framework's default context assembly.</returns>
    string? GetMigrationAssemblyName(Type dbContextType, string databaseProvider);
}
