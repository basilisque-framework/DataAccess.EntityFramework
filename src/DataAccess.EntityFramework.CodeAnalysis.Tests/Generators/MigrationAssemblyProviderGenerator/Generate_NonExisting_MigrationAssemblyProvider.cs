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

using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;
using Microsoft.CodeAnalysis.Testing;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.MigrationAssemblyProviderGenerator;

[InheritsTests]
[Category(MigrationAssemblyProviderGeneratorCategory)]
public class Generate_NonExisting_MigrationAssemblyProvider : BaseDataAccessEntityFrameworkGeneratorTest
{
    protected override void AddSourcesUnderTest(SourceFileList sources)
    {
        sources.Add(@"
using Microsoft.EntityFrameworkCore.Design;
[assembly:DesignTimeServicesReferenceAttribute(""Basilisque.DataAccess.EntityFramework.Relational.Design.RelationalDesignTimeServices, Basilisque.DataAccess.EntityFramework.Relational"")]
");
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources()
    {
        yield break;
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedAttributeSources(IReadOnlyDictionary<string, (string CompilationName, string Source)> supportedAttributes)
    {
        yield break;
    }

    protected override (string Name, string SourceText)? GetExpectedMigrationAssemblyProviderSources(string migrationAssemblyProviderCompilationName, string migrationAssemblyProviderSource)
    {
        return (
            Name: MigrationAssemblyProviderGeneratorData.MigrationAssemblyProviderCompilationName,
            SourceText: MigrationAssemblyProviderGeneratorData.MigrationAssemblyProviderSource
            );
    }
}
