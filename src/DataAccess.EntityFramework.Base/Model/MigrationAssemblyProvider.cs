namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>Base class for generated, context-specific migration assembly mappings.</summary>
public abstract class MigrationAssemblyProvider : IMigrationAssemblyProvider
{
    /// <inheritdoc />
    public abstract string? GetMigrationAssemblyName(Type dbContextType, string databaseProvider);
}
