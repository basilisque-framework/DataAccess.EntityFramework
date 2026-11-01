namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>An explicit migration assembly selection for one context/provider pair.</summary>
/// <param name="DbContextType">The concrete context type.</param>
/// <param name="DatabaseProvider">The Basilisque database provider key.</param>
/// <param name="AssemblyName">The migration assembly's simple name.</param>
public sealed record MigrationAssemblyOverride(Type DbContextType, string DatabaseProvider, string AssemblyName);
