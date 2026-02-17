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

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

/// <summary>
/// Provides data necessary for generating attributes required for entity framework design time services.
/// </summary>
public static class DesignTimeServicesAttributeGeneratorData
{
    private const string DesignTimeServicesAttributeCompilationName = "Basilisque_DataAccess_EntityFramework_DesignTimeServicesAttribute.g.cs";

    private static readonly string _designTimeServicesAttributeSource = $@"{CommonGeneratorData.GeneratedFileSharedHeaderWithNullable}
[assembly:Microsoft.EntityFrameworkCore.Design.DesignTimeServicesReference(""Basilisque.DataAccess.EntityFramework.Relational.Design.RelationalDesignTimeServices, Basilisque.DataAccess.EntityFramework.Relational"")]
";

    /// <summary>
    /// The list of currently supported design time services attributes.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string CompilationName, string Source)> SupportedDesignTimeServicesAttributes = new Dictionary<string, (string CompilationName, string Source)>()
    {
        { "global::Microsoft.EntityFrameworkCore.Design.DesignTimeServicesReferenceAttribute", (DesignTimeServicesAttributeCompilationName, _designTimeServicesAttributeSource) },
    };
}
