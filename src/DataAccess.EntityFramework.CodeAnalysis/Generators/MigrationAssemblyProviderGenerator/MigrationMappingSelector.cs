using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeDbContextFactoryGenerator;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

internal static class MigrationMappingSelector
{
    private static readonly DiagnosticDescriptor AmbiguousContext = new(
        "BAS_EF001", "Migration context is ambiguous",
        "Migration assembly '{0}' references multiple contexts but contains no context or migration metadata. Add a migration for the intended context, or register its assembly explicitly with AddMigrationAssembly<TDbContext>().",
        "Basilisque.EntityFramework", DiagnosticSeverity.Warning, true);

    internal static string GetMappings(SourceProductionContext context, Compilation compilation, BuildPropertyInfo properties)
    {
        var baseType = compilation.GetTypeByMetadataName("Basilisque.DataAccess.EntityFramework.Base.Model.BaseDbContext`1");
        var factoryType = compilation.GetTypeByMetadataName("Basilisque.DataAccess.EntityFramework.Base.Design.BaseDesignTimeDbContextFactory`1");
        if (baseType is null || factoryType is null)
            return "";
        var collector = new BaseDbContextImplementationsCollector(context.CancellationToken, baseType, factoryType);
        collector.VisitNamespace(compilation.GlobalNamespace);
        var candidates = collector.GetDbContextImplementations();

        // Only metadata physically declared in this assembly establishes migration ownership.
        var contexts = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var type in getTypes(compilation.Assembly.GlobalNamespace))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute"
                    && attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is INamedTypeSymbol mappedContext)
                    contexts.Add(mappedContext);
            }
        }
        if (contexts.Count == 0)
        {
            foreach (var candidate in candidates.Where(item => item.Locations.Any(location => location.IsInSource)))
                contexts.Add(candidate);
            if (contexts.Count == 0 && candidates.Length == 1)
                contexts.Add(candidates[0]);
            else if (contexts.Count == 0 && candidates.Length > 1)
                context.ReportDiagnostic(Diagnostic.Create(AmbiguousContext, Location.None, compilation.AssemblyName));
        }

        var lines = new List<string>();
        foreach (var mappedContext in contexts.OrderBy(item => item.ToDisplayString(), StringComparer.Ordinal))
        foreach (var provider in properties.DatabaseProviders)
        {
            var providerLiteral = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(provider, true);
            lines.Add($"        if (dbContextType == typeof({mappedContext.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}) && databaseProvider == {providerLiteral})");
            lines.Add("            return typeof(MigrationAssemblyProvider).Assembly.GetName().Name;");
        }
        return string.Join("\r\n", lines);
    }

    private static IEnumerable<INamedTypeSymbol> getTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        foreach (var item in getNestedTypes(type))
            yield return item;
        foreach (var child in ns.GetNamespaceMembers())
        foreach (var type in getTypes(child))
            yield return type;
    }

    private static IEnumerable<INamedTypeSymbol> getNestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var child in type.GetTypeMembers())
        foreach (var nested in getNestedTypes(child))
            yield return nested;
    }
}
