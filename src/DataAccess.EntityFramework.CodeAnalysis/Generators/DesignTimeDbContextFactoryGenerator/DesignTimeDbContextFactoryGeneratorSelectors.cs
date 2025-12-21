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

internal static class DesignTimeDbContextFactoryGeneratorSelectors
{
    internal static IncrementalValueProvider<DesignTimeDbContextFactoryGeneratorInfo> GetDbContextsToGenerate(IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(getGeneratorInfo);
    }

    private static DesignTimeDbContextFactoryGeneratorInfo getGeneratorInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typeCollector = createTypeCollector(compilation, cancellationToken);

        var dbContextsToGenerate = typeCollector?.GetDbContextImplementations() ?? ImmutableArray<INamedTypeSymbol>.Empty;
        var designTimeFactoriesToGenerate = typeCollector?.GetDbContextDesignTimeFactories() ?? ImmutableArray<INamedTypeSymbol>.Empty;

        return new DesignTimeDbContextFactoryGeneratorInfo(dbContextsToGenerate, designTimeFactoriesToGenerate);
    }

    private static BaseDbContextImplementationsCollector? createTypeCollector(Compilation compilation, CancellationToken cancellationToken)
    {
        var baseDbContextType = compilation.GetTypeByMetadataName("Basilisque.DataAccess.EntityFramework.Base.Model.BaseDbContext`1");
        if (baseDbContextType is null)
            return null;

        var baseDesignTimeFactoryType = compilation.GetTypeByMetadataName("Basilisque.DataAccess.EntityFramework.Base.Design.BaseDesignTimeDbContextFactory`1");
        if (baseDesignTimeFactoryType is null)
            return null;

        var collector = new BaseDbContextImplementationsCollector(cancellationToken, baseDbContextType, baseDesignTimeFactoryType);
        collector.VisitNamespace(compilation.GlobalNamespace);

        return collector;
    }
}