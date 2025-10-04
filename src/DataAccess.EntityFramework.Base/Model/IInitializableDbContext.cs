namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Implementing this interface provides support for DbContexts to be initialized.
/// </summary>
public interface IInitializableDbContext
{
    /// <summary>
    /// Indicates whether the current DbContext was already migrated.
    /// </summary>
    /// <remarks>Internally saves this state in a <see langword="static"/> field.</remarks>
    /// <returns>A boolean value indicating whether the current DbContext was already migrated.</returns>
    bool WasAlreadyMigrated { get; set; }
}
