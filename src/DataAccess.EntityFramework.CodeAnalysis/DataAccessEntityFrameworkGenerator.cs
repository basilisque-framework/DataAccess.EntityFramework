/*
   Copyright 2025-2026 Alexander Stärk

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

using Basilisque.CodeAnalysis.Syntax;
using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators;
using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeDbContextFactoryGenerator;
using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;
using Basilisque.DependencyInjection.CodeAnalysis.ExtensionSupport.Common;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis;

/// <summary>
/// A source generator that generates code for Basilisque.DataAccess design time support for Entity Framework.
/// </summary>
[Generator]
public class DataAccessEntityFrameworkGenerator : IIncrementalGenerator
{
    ///<inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildPropertiesSelector = context.AnalyzerConfigOptionsProvider.Select(Generators.CommonGeneratorSelectors.BuildPropertiesSelector);

        var diExtensionValueProvider = context.GetDependencyInjectionExtensionValueProvider();

        initializeDesignTimeServicesAttributeGenerator(context, buildPropertiesSelector);

        initializeDesignTimeDbContextFactoryGenerator(context, buildPropertiesSelector, diExtensionValueProvider);

        initializeMigrationAssemblyProviderGenerator(context, buildPropertiesSelector, diExtensionValueProvider);
    }

    private void initializeDesignTimeServicesAttributeGenerator(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<BuildPropertyInfo> buildPropertiesSelector)
    {
        var existingAttributesProvider = DesignTimeServicesAttributeGeneratorSelectors.GetExistingAttributes(context).Collect();

        context.RegisterSourceOutput(existingAttributesProvider.Combine(buildPropertiesSelector), DesignTimeServicesAttributeGeneratorOutput.OutputAttributes);
    }

    private void initializeDesignTimeDbContextFactoryGenerator(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<BuildPropertyInfo> buildPropertiesSelector, IncrementalValueProvider<(string? RootNamespace, string? AssemblyName)> diExtensionValueProvider)
    {
        var classesToGenerateProvider = DesignTimeDbContextFactoryGeneratorSelectors.GetDbContextsToGenerate(context);

        var combinedProvider = classesToGenerateProvider.Combine(buildPropertiesSelector).Combine(diExtensionValueProvider);

        context.RegisterCompilationInfoOutput(combinedProvider, DesignTimeDbContextFactoryGeneratorOutput.OutputImplementations);
    }

    private void initializeMigrationAssemblyProviderGenerator(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<BuildPropertyInfo> buildPropertiesSelector, IncrementalValueProvider<(string? RootNamespace, string? AssemblyName)> diExtensionValueProvider)
    {
        var existingMigrationAssembliyProvidersProvider = MigrationAssemblyProviderGeneratorSelectors.HasExistingMigrationAssemblyProvider(context);

        var combinedProvider = existingMigrationAssembliyProvidersProvider.Combine(buildPropertiesSelector).Combine(diExtensionValueProvider);

        context.RegisterCompilationInfoOutput(combinedProvider, MigrationAssemblyProviderGeneratorOutput.OutputMigrationAssemblyProviders);
    }
}
