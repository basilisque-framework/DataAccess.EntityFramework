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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.Common;

[InheritsTests]
[Category(CommonCategory)]
public class DoNotGenerate_WhenDisabled : BaseDataAccessEntityFrameworkGeneratorTest
{
    protected override void AddSourcesUnderTest(SourceFileList sources)
    {
        // do not add an attribute here to ensure the generator would generate it if it wasn't enabled

        sources.Add("// workaround for 'TestState.Sources' is empty");
    }

    protected override IEnumerable<(string AssemblyName, IReadOnlyList<string> Sources, IReadOnlyList<MetadataReference> DbContextAssemblyReferences)> GetDbContextsUnderTest()
    {
        // add a DbContext to ensure the generator would generate code for it if it wasn't disabled

        yield return ("DbContextAssembly", [@"
#nullable enable

namespace DbContextAssembly.DataAccess.EFG.TestObjects.DoNotGenerate_WhenDisabled;

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
"], []);
    }

    protected override IEnumerable<MetadataReference> GetMigrationAssemblyReferences()
    {
        yield return ReferenceAssembly_SqlServerBaseDesignTimeDbContextFactory;
    }


    protected override IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources()
    {
        // since the generator is disabled, no DbContextFactory source is expected
        yield break;
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedAttributeSources(IReadOnlyDictionary<string, (string CompilationName, string Source)> supportedAttributes)
    {
        // since the generator is disabled, no attribute source is expected
        yield break;
    }

    protected override IEnumerable<(string filename, string content)> GetAnalyzerConfigFiles()
    {
        var configFiles = base.GetAnalyzerConfigFiles();

        foreach (var file in configFiles)
        {
            // remote BAS_DA_EF_IsMigrationAssembly setting from the editorconfig to disable the generator
            if (file.filename == "/.editorconfig")
            {
                yield return (file.filename, file.content.Replace("build_property.BAS_DA_EF_IsMigrationAssembly = true", ""));
                continue;
            }

            yield return file;
        }
    }
}

