/*
   Copyright 2025 Alexander Stärk

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

using System.Collections.Immutable;
using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeDbContextFactoryGenerator;

internal class BaseDbContextImplementationsCollector : SymbolVisitor
{
    private readonly HashSet<INamedTypeSymbol> _foundDbContextTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    private readonly HashSet<INamedTypeSymbol> _foundDbContextDesignTimeFactoryTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    private readonly INamedTypeSymbol _baseDbContextType;
    private readonly INamedTypeSymbol _baseDbContextDesignTimeFactoryType;
    private readonly CancellationToken _cancellationToken;

    public BaseDbContextImplementationsCollector(CancellationToken cancellationToken, INamedTypeSymbol baseDbContextType, INamedTypeSymbol baseDbContextDesignTimeFactoryType)
    {
        _cancellationToken = cancellationToken;
        _baseDbContextType = baseDbContextType;
        _baseDbContextDesignTimeFactoryType = baseDbContextDesignTimeFactoryType;
    }

    public ImmutableArray<INamedTypeSymbol> GetDbContextImplementations() => _foundDbContextTypes.ToImmutableArray();

    public ImmutableArray<INamedTypeSymbol> GetDbContextDesignTimeFactories() => _foundDbContextDesignTimeFactoryTypes.ToImmutableArray();

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (INamespaceOrTypeSymbol namespaceOrType in symbol.GetMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();

            namespaceOrType.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol type)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!isRealUserType(type))
            return;

        if (!isSourceType(type) && !isReferencedType(type))
            return;

        if (type.BaseType is null)
            return;

        var ns = type.ContainingNamespace.ToDisplayString();
        if (ns.StartsWith("Microsoft") || ns.StartsWith("System"))
            return;

        if (inheritsFromBaseDbContext(type))
        {
            if (type.IsAbstract)
                return;

            if (!_foundDbContextTypes.Add(type))
                return;
        }
        else if (inheritsFromBaseDbContextDesignTimeFactory(type))
        {
            if (!_foundDbContextDesignTimeFactoryTypes.Add(type))
                return;
        }
        else
            return;

        var nestedTypes = type.GetTypeMembers();
        if (nestedTypes.IsDefaultOrEmpty)
            return;

        foreach (INamedTypeSymbol nestedType in nestedTypes)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            nestedType.Accept(this);
        }
    }

    private bool isSourceType(INamedTypeSymbol type) => type.Locations.Any(l => l.IsInSource) && isRealUserType(type);

    private bool isReferencedType(INamedTypeSymbol type)
    {
        if (type.Locations.Any(l => l.IsInSource))
            return false;

        return isRealUserType(type)
               && (type.DeclaredAccessibility == Accessibility.Public
                   || isAccessibleInThisAssembly(type));
    }

    private bool isRealUserType(INamedTypeSymbol t)
    {
        // exclude compiler-types (<Module>, <AnonymousType>, ...)
        if (t.Name.StartsWith("<"))
            return false;

        if (t.IsImplicitlyDeclared)
            return false;

        if (t.TypeKind != TypeKind.Class)
            return false;

        return true;
    }

    private bool isAccessibleInThisAssembly(INamedTypeSymbol type)
    {
        // public types are accessible
        if (type.DeclaredAccessibility == Accessibility.Public)
            return true;

        // internal + InternalsVisibleTo is also accessible
        if (type.DeclaredAccessibility == Accessibility.Internal ||
            type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
        {
            var asm = type.ContainingAssembly;
            var visibleTo = asm.GetAttributes().Where(a => a.AttributeClass?.Name == "InternalsVisibleToAttribute");

            foreach (var attr in visibleTo)
            {
                var arg = attr.ConstructorArguments.FirstOrDefault().Value as string;
                if (!string.IsNullOrEmpty(arg) && arg!.Contains("TheNameOfTheAssemblyThatThisGeneratorIsCurrentlyRunningFor"))
                    return true;
            }
        }

        return false;
    }

    private bool inheritsFromBaseDbContext(INamedTypeSymbol symbol)
    {
        return inheritsFrom(symbol, _baseDbContextType);
    }

    private bool inheritsFromBaseDbContextDesignTimeFactory(INamedTypeSymbol symbol)
    {
        return inheritsFrom(symbol, _baseDbContextDesignTimeFactoryType);
    }

    private bool inheritsFrom(INamedTypeSymbol symbol, INamedTypeSymbol baseType)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.ConstructedFrom, baseType))
                return true;

            current = current.BaseType;
        }
        return false;
    }
}