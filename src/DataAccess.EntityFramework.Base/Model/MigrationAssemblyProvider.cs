namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <inheritdoc />
public abstract class MigrationAssemblyProvider : IMigrationAssemblyProvider
{
    /// <inheritdoc />
    public virtual string? GetMigrationAssemblyName()
    {
        return GetMigrationAssembly()?.GetName().Name;
    }

    /// <summary>
    /// Gets the assembly that contains the database migrations.
    /// </summary>
    /// <returns>The <see cref="System.Reflection.Assembly"/> instance representing the assembly containing the database migrations, or <see langword="null"/> for the default Entity Framework behaviour.</returns>
    public virtual System.Reflection.Assembly? GetMigrationAssembly()
    {
        return this.GetType().Assembly;
    }
}
