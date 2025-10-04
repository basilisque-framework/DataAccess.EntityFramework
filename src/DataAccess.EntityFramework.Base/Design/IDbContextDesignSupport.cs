namespace Basilisque.DataAccess.EntityFramework.Base.Design;

/// <summary>
/// Provides support for determining wheter the current database context instance is being used in a design-time environment.
/// </summary>
public interface IDbContextDesignSupport
{
    /// <summary>
    /// Gets or sets a value indicating whether the current database context instance is being used in a design-time environment.
    /// </summary>
    bool IsDesignTime { get; set; }
}
