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
using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;
using Basilisque.DependencyInjection.CodeAnalysis.ExtensionSupport.DependencyInjectionGenerator;
using System.Collections.Immutable;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeDbContextFactoryGenerator;

internal static class DesignTimeDbContextFactoryGeneratorOutput
{
    private static SymbolDisplayFormat _baseDesignTimeFactorySymbolDisplayFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None);

    private static SymbolDisplayFormat _fullyQualifiedFormatWithoutGlobalNamespace = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    internal static void OutputImplementations(SourceProductionContext context, ((DesignTimeDbContextFactoryGeneratorInfo GeneratorInfo, BuildPropertyInfo BuildProperties) Input, (string? RootNamespace, string? AssemblyName) DIInfo) data, RegistrationOptions registrationOptions)
    {
        if (!data.Input.BuildProperties.IsMigrationAssembly)
            return;

        if (!checkPreconditions(registrationOptions))
            return;

        var baseDesignTimeFactoryFQNames = getActiveDesignTimeFactories(data.Input.GeneratorInfo.DesignTimeFactories, data.Input.BuildProperties.DesignTimeDbContextFactories);

        if (baseDesignTimeFactoryFQNames.Length < 1 || data.Input.GeneratorInfo.DbContexts.Length < 1)
            return;

        if (data.Input.GeneratorInfo.DesignTimeFactories.Length > 1)
            outputMultipleProviders(context, registrationOptions, data.Input.GeneratorInfo.DbContexts, baseDesignTimeFactoryFQNames, data.Input.BuildProperties);
        else
            outputSingleProvider(context, registrationOptions, data.Input.GeneratorInfo.DbContexts, baseDesignTimeFactoryFQNames[0], data.Input.BuildProperties);

        var diExtensionData = (data.Input.GeneratorInfo.DbContexts, data.DIInfo);
        DependencyInjectionExtensionGeneratorOutput.OutputImplementations(context, diExtensionData, registrationOptions, "BAS_DA_EF_DIExt_DBC", registerExtensionCallback: outputDependencyInjectionExtension);
    }

    private static ImmutableArray<string> getActiveDesignTimeFactories(ImmutableArray<INamedTypeSymbol> designTimeFactories, string[]? designTimeDbContextFactories)
    {
        var baseDesignTimeFactoryFQName = designTimeFactories.Select(s => s.ToDisplayString(_baseDesignTimeFactorySymbolDisplayFormat));

        return baseDesignTimeFactoryFQName.Where(f => designTimeDbContextFactories.Contains(f)).OrderBy(s => s).ToImmutableArray();
    }

    private static void outputSingleProvider(SourceProductionContext context, RegistrationOptions registrationOptions, ImmutableArray<INamedTypeSymbol> dbContexts, string baseDesignTimeFactoryFQName, BuildPropertyInfo buildProperties)
    {
        outputProvider(context, registrationOptions, dbContexts, (fullQualifiedDbContextName, classInfo) =>
        {
            classInfo.BaseClass = $"{baseDesignTimeFactoryFQName}<{fullQualifiedDbContextName}>";

            addMigrationAssemblyProviderRegistration(classInfo, buildProperties);
        });
    }

    private static void outputMultipleProviders(SourceProductionContext context, RegistrationOptions registrationOptions, ImmutableArray<INamedTypeSymbol> dbContexts, ImmutableArray<string> baseDesignTimeFactoryFQNames, BuildPropertyInfo buildProperties)
    {
        outputProvider(context, registrationOptions, dbContexts, (fullQualifiedDbContextName, classInfo) =>
        {
            classInfo.BaseClass = $"global::Basilisque.DataAccess.EntityFramework.Base.Design.BaseDesignTimeDbContextFactory<{fullQualifiedDbContextName}>";

            var m = new MethodInfo(AccessModifier.Public, "sealed override void", "ConfigureProviderServices")
            {
                InheritXmlDoc = true
            };

            m.Parameters.Add(new ParameterInfo(ParameterKind.Ordinary, "global::Microsoft.Extensions.DependencyInjection.IServiceCollection", "services"));
            m.Parameters.Add(new ParameterInfo(ParameterKind.Ordinary, "string[]", "args"));
            m.Parameters.Add(new ParameterInfo(ParameterKind.Ordinary, "bool", "isChildFactory"));

            int i = 0;
            foreach (var baseDesignTimeFactoryFQName in baseDesignTimeFactoryFQNames)
            {
                i++;
                var factoryFieldName = $"_designTimeFactory{i}";

                m.Body.Add($"{factoryFieldName}.ConfigureProviderServices(services, args, isChildFactory: true);");

                classInfo.Fields.Add(new FieldInfo($"{baseDesignTimeFactoryFQName}<{fullQualifiedDbContextName}>", factoryFieldName, $"new()"));
            }

            classInfo.Methods.Add(m);

            addMigrationAssemblyProviderRegistration(classInfo, buildProperties);
        });
    }

    private static void addMigrationAssemblyProviderRegistration(ClassInfo classInfo, BuildPropertyInfo buildProperties)
    {
        var migrationAssemblyProviderNamespace = MigrationAssemblyProviderGeneratorData.GetMigrationAssemblyProviderNamespace(buildProperties);

        var m = new MethodInfo(AccessModifier.Protected, "sealed override void", "ConfigureMigrationAssemblyProvider")
        {
            InheritXmlDoc = true
        };

        m.Parameters.Add(new ParameterInfo(ParameterKind.Ordinary, "global::Microsoft.Extensions.DependencyInjection.IServiceCollection", "services"));
        m.Parameters.Add(new ParameterInfo(ParameterKind.Ordinary, "string[]", "args"));

        m.Body.Add($"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<global::Basilisque.DataAccess.EntityFramework.Base.Model.IMigrationAssemblyProvider, global::{migrationAssemblyProviderNamespace}.MigrationAssemblyProvider>(services);");

        classInfo.Methods.Add(m);
    }

    private static void outputProvider(SourceProductionContext context, RegistrationOptions registrationOptions, ImmutableArray<INamedTypeSymbol> dbContexts, Action<string, ClassInfo> classConfiguration)
    {
        foreach (var dbContextSymbol in dbContexts)
        {
            var fullQualifiedName = dbContextSymbol.ToDisplayString(_fullyQualifiedFormatWithoutGlobalNamespace);

            var className = dbContextSymbol.Name;
            var namespaceName = dbContextSymbol.ContainingNamespace.ToDisplayString();

            var compilationName = $"{fullQualifiedName}DesignTimeFactory";

            var ci = registrationOptions.CreateCompilationInfo(compilationName, namespaceName);
            ci.EnableNullableContext = true;

            ci.AddNewClassInfo($"{className}DesignTimeFactory", AccessModifier.Public, cl =>
            {
                cl.IsPartial = true;
                cl.InheritXmlDoc = true;

                classConfiguration(fullQualifiedName, cl);

            }).AddToSourceProductionContext();
        }
    }

    private static bool checkPreconditions(RegistrationOptions registrationOptions)
    {
        if (registrationOptions.Language != Language.CSharp)
            throw new System.NotSupportedException($"The language '{registrationOptions.Language}' is currently not supported by this generator.");

        return true;
    }

    private static void outputDependencyInjectionExtension(SourceProductionContext context, CodeLines registrationMethodBody, ImmutableArray<INamedTypeSymbol> implementationParameters)
    {
        foreach (var dbContextSymbol in implementationParameters)
        {
            var fullQualifiedName = dbContextSymbol.ToDisplayString(_fullyQualifiedFormatWithoutGlobalNamespace);
            registrationMethodBody.Add($"services.AddDbContext<global::{fullQualifiedName}>();");
        }
    }
}
