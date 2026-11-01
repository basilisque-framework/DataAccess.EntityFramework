namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <inheritdoc />
public sealed class MigrationAssemblyResolver : IMigrationAssemblyResolver
{
    private readonly IMigrationAssemblyProvider[] _providers;
    private readonly MigrationAssemblyOverride[] _overrides;

    /// <summary>Creates a resolver from generated mappings and explicit overrides.</summary>
    /// <param name="providers">The registered mapping providers.</param>
    /// <param name="overrides">The explicitly configured selections.</param>
    public MigrationAssemblyResolver(IEnumerable<IMigrationAssemblyProvider> providers, IEnumerable<MigrationAssemblyOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(overrides);
        _providers = providers.ToArray();
        _overrides = overrides.ToArray();
    }

    /// <inheritdoc />
    public string? GetMigrationAssemblyName(Type dbContextType, string databaseProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContextType);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseProvider);

        var explicitNames = _overrides
            .Where(item => item.DbContextType == dbContextType && item.DatabaseProvider == databaseProvider)
            .Select(item => item.AssemblyName).Distinct(StringComparer.Ordinal).ToArray();
        var names = explicitNames.Length > 0 ? explicitNames : _providers
            .Select(item => item.GetMigrationAssemblyName(dbContextType, databaseProvider))
            .Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>()
            .Distinct(StringComparer.Ordinal).ToArray();

        if (names.Length > 1)
            throw new InvalidOperationException($"Multiple migration assemblies match context '{dbContextType.FullName}' and provider '{databaseProvider}': {string.Join(", ", names.OrderBy(name => name, StringComparer.Ordinal))}. Register one explicit selection with AddMigrationAssembly<TDbContext>().");

        return names.SingleOrDefault();
    }
}
