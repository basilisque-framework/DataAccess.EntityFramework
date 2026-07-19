/*
   Copyright 2026-2026 Alexander Stärk

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
using Basilisque.DependencyInjection.CodeAnalysis.ExtensionSupport.DependencyInjectionGenerator;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

internal static class MigrationAssemblyProviderGeneratorOutput
{
    internal static void OutputMigrationAssemblyProviders(SourceProductionContext context, ((bool HasExistingMigrationAssemblyProvider, BuildPropertyInfo BuildProperties) Input, (string? RootNamespace, string? AssemblyName) DIInfo) data, RegistrationOptions registrationOptions)
    {
        if (!data.Input.BuildProperties.IsMigrationAssembly)
            return;

        if (data.Input.HasExistingMigrationAssemblyProvider)
            return;

        var outputNamespace = MigrationAssemblyProviderGeneratorData.GetMigrationAssemblyProviderNamespace(data.Input.BuildProperties);

        context.AddSource(MigrationAssemblyProviderGeneratorData.MigrationAssemblyProviderCompilationName, MigrationAssemblyProviderGeneratorData.GetMigrationAssemblyProviderSource(outputNamespace));

        var migrationAssemblyProviderFQN = $"{outputNamespace}.MigrationAssemblyProvider";

        var diExtensionData = (migrationAssemblyProviderFQN, data.DIInfo);

        DependencyInjectionExtensionGeneratorOutput.OutputImplementations(context, diExtensionData, registrationOptions, "BAS_DA_EF_DIExt_MAP", registerExtensionCallback: outputDependencyRegistrationExtension);
    }

    private static void outputDependencyRegistrationExtension(SourceProductionContext context, CodeLines registrationMethodBody, string migrationAssemblyProviderFQN)
    {
        registrationMethodBody.Add($"services.AddTransient<global::Basilisque.DataAccess.EntityFramework.Base.Model.IMigrationAssemblyProvider, global::{migrationAssemblyProviderFQN}>();");
    }
}
