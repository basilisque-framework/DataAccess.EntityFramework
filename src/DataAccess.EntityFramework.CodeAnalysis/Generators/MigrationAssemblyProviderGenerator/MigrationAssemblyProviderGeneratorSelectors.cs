/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

internal static class MigrationAssemblyProviderGeneratorSelectors
{
    internal static IncrementalValueProvider<bool> HasExistingMigrationAssemblyProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, cancellationToken) => hasExistingMigrationAssemblyProvider(compilation, cancellationToken));
    }

    private static bool hasExistingMigrationAssemblyProvider(Compilation compilation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var migrationAssemblyProviderInterface = compilation.GetTypeByMetadataName("Basilisque.DataAccess.EntityFramework.Base.Model.IMigrationAssemblyProvider");
        if (migrationAssemblyProviderInterface is null)
            return false;

        foreach (var type in getAllTypes(compilation.Assembly.GlobalNamespace))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type.TypeKind != TypeKind.Class || type.IsAbstract)
                continue;

            if (type.AllInterfaces.Contains(migrationAssemblyProviderInterface, SymbolEqualityComparer.Default))
                return true;
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> getAllTypes(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetTypeMembers())
        {
            yield return member;

            foreach (var nested in getNestedTypes(member))
                yield return nested;
        }

        foreach (var nestedNamespace in ns.GetNamespaceMembers())
        {
            foreach (var type in getAllTypes(nestedNamespace))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> getNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;

            foreach (var child in getNestedTypes(nested))
                yield return child;
        }
    }
}
