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

using Microsoft.CodeAnalysis.Testing;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.DesignTimeDbContextFactoryGenerator;

[InheritsTests]
[Category(DesignTimeDbContextFactoryGeneratorCategory)]
public class Generate_DbContextDependencyInjectionExtension_InRelationalAssembly : BaseDataAccessEntityFrameworkGeneratorTest
{
    protected override void AddSourcesUnderTest(SourceFileList sources)
    {
        sources.Add(@"
#nullable enable

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.TestObjects.Relational;

/// <summary>
/// Test DbContext
/// </summary>
public class MyDbContext : Basilisque.DataAccess.EntityFramework.Relational.Model.BaseDbContext<MyDbContext>
{
    /// <summary>
    /// Creates a new MyDbContext.
    /// </summary>
    public MyDbContext(Basilisque.DataAccess.EntityFramework.Base.DependencyInjection.IDbProviderServiceProvider dbProviderServiceProvider)
        : base(dbProviderServiceProvider)
    { }

    /// <inheritdoc />
    protected override string GetAppAreaPrefix()
    {
        return ""MyAppArea"";
    }
}
");
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources()
    {
        yield break;
    }

    protected override IEnumerable<string> GetExpectedDbContextDependencyInjectionRegistrations()
    {
        yield return "Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.TestObjects.Relational.MyDbContext";
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedAttributeSources(IReadOnlyDictionary<string, (string CompilationName, string Source)> supportedAttributes)
    {
        yield break;
    }

    protected override (string Name, string SourceText)? GetExpectedMigrationAssemblyProviderSources(string migrationAssemblyProviderCompilationName, string migrationAssemblyProviderSource)
    {
        return null;
    }

    protected override bool ExpectMigrationAssemblyProviderDependencyInjectionExtension()
    {
        return false;
    }

    protected override IEnumerable<(string filename, string content)> GetAnalyzerConfigFiles()
    {
        foreach (var file in base.GetAnalyzerConfigFiles())
        {
            if (file.filename == "/.editorconfig")
            {
                yield return (file.filename, file.content.Replace("build_property.BAS_DA_EF_IsMigrationAssembly = true", "build_property.BAS_DA_EF_IsMigrationAssembly = false"));
                continue;
            }

            yield return file;
        }
    }
}
