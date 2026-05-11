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

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

/// <summary>
/// Provides data necessary for generating a migration assembly provider implementation.
/// </summary>
public static class MigrationAssemblyProviderGeneratorData
{
    /// <summary>
    /// The file name used for the generated migration assembly provider implementation.
    /// </summary>
    public const string MigrationAssemblyProviderCompilationName = "Basilisque_DataAccess_EntityFramework_MigrationAssemblyProvider.g.cs";

    /// <summary>
    /// The source code for the generated migration assembly provider implementation.
    /// </summary>
    public static readonly string MigrationAssemblyProviderSource = $@"{CommonGeneratorData.GeneratedFileSharedHeaderWithNullable}
/// <summary>
/// <inheritdoc cref=""global::Basilisque.DataAccess.EntityFramework.Base.Model.MigrationAssemblyProvider"" />
/// </summary>
public partial class MigrationAssemblyProvider : global::Basilisque.DataAccess.EntityFramework.Base.Model.MigrationAssemblyProvider
{{
}}
";
}
