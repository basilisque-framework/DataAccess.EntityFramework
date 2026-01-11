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

using Basilisque.CodeAnalysis.TestSupport.SourceGenerators.UnitTests.Verifiers;
using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators;

public class BaseDataAccessEntityFrameworkGeneratorVerifier : IncrementalSourceGeneratorVerifier<DataAccessEntityFrameworkGenerator>
{
}

[InheritsTests]
public abstract class BaseDataAccessEntityFrameworkGeneratorTest : BaseDataAccessEntityFrameworkGeneratorTest<BaseDataAccessEntityFrameworkGeneratorVerifier>
{
    protected const string CommonCategory = "Common";
    protected const string DesignTimeServicesAttributeGeneratorCategory = "DesignTimeServicesAttributeGenerator";
    protected const string DesignTimeDbContextFactoryGeneratorCategory = "DesignTimeDbContextFactoryGenerator";
}

[InheritsTests]
public abstract class BaseDataAccessEntityFrameworkGeneratorTest<TVerifier> : BaseDataAccessEntityFrameworkGeneratorTest<DataAccessEntityFrameworkGenerator, TVerifier>
    where TVerifier : IncrementalSourceGeneratorVerifier<DataAccessEntityFrameworkGenerator>, new()
{ }

public abstract class BaseDataAccessEntityFrameworkGeneratorTest<TGenerator, TVerifier>
    where TGenerator : Microsoft.CodeAnalysis.IIncrementalGenerator, new()
    where TVerifier : IncrementalSourceGeneratorVerifier<TGenerator>, new()
{
    public virtual Microsoft.CodeAnalysis.CSharp.LanguageVersion? LanguageVersion { get { return null; } }

    protected static MetadataReference ReferenceAssembly_SqlServerBaseDesignTimeDbContextFactory = MetadataReference.CreateFromFile(typeof(Basilisque.DataAccess.EntityFramework.SqlServer.Design.SqlServerBaseDesignTimeDbContextFactory<>).Assembly.Location);
    protected static MetadataReference ReferenceAssembly_SQLiteBaseDesignTimeDbContextFactory = MetadataReference.CreateFromFile(typeof(Basilisque.DataAccess.EntityFramework.SQLite.Design.SQLiteBaseDesignTimeDbContextFactory<>).Assembly.Location);
    protected static MetadataReference ReferenceAssembly_PostgreSQLBaseDesignTimeDbContextFactory = MetadataReference.CreateFromFile(typeof(Basilisque.DataAccess.EntityFramework.PostgreSQL.Design.PostgreSQLBaseDesignTimeDbContextFactory<>).Assembly.Location);
    protected static IReadOnlyList<MetadataReference> ReferenceAssemblies_AllBaseDesignTimeDbContextFactories = [
        ReferenceAssembly_SqlServerBaseDesignTimeDbContextFactory,
        ReferenceAssembly_SQLiteBaseDesignTimeDbContextFactory,
        ReferenceAssembly_PostgreSQLBaseDesignTimeDbContextFactory
        ];

    [Test]
    public virtual async Task Test()
    {
        var verifier = GetVerifier();

        var expectedSources = getExpectedSources();
        foreach (var expectedSource in expectedSources)
        {
            var name = expectedSource.Name;

            if (!name.EndsWith(".cs"))
                name = $"{name}.cs";

            verifier.TestState.GeneratedSources.Add((typeof(TGenerator), name, SourceText.From(expectedSource.SourceText, Encoding.UTF8)));
        }

        await verifier.RunAsync();
    }

    protected virtual TVerifier GetVerifier()
    {
        //define reference assemblies
        var refAssemblies = GetReferenceAssemblies();

        //create verifier
        var verifier = new TVerifier()
        {
            ReferenceAssemblies = refAssemblies,
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly,
                    typeof(Basilisque.DataAccess.EntityFramework.Relational.Model.BaseDbContext<>).Assembly,
                    typeof(Basilisque.DataAccess.EntityFramework.Base.Model.BaseDbContext<>).Assembly
                }
            }
        };

        foreach (var item in getDbContextsAssembliesUnderTest())
            verifier.TestState.AdditionalReferences.Add(item);

        foreach (var item in GetMigrationAssemblyReferences())
            verifier.TestState.AdditionalReferences.Add(item);

        if (LanguageVersion.HasValue)
            verifier.LanguageVersion = LanguageVersion.Value;

        var assemblyNameInfo = System.Reflection.Assembly.GetAssembly(typeof(DataAccessEntityFrameworkGenerator))?.GetName();

        string assemblyName, version;
        if (assemblyNameInfo is null)
        {
            version = "1.0.0.0";
            assemblyName = "Basilisque.DataAccess.EntityFramework.CodeAnalysis-1.0-Alpha";
        }
        else
        {
            version = assemblyNameInfo.Version?.ToString() ?? "1.0.0.0";
            assemblyName = assemblyNameInfo.Name ?? "Basilisque.DataAccess.EntityFramework.CodeAnalysis-1.0-Alpha";
        }

        //add analyzer config files
        foreach (var file in GetAnalyzerConfigFiles())
            verifier.TestState.AnalyzerConfigFiles.Add(file);

        //set the diagnostic options
        foreach (var diagOp in GetDiagnosticOptions())
            verifier.DiagnosticOptions.Add(diagOp.key, diagOp.value);

        foreach (var expDiag in GetExpectedDiagnostics(assemblyName, version))
            verifier.ExpectedDiagnostics.Add(expDiag);

        //add the sources
        AddSourcesUnderTest(verifier.TestState.Sources);

        return verifier;
    }

    protected virtual IEnumerable<(string AssemblyName, IReadOnlyList<string> Sources, IReadOnlyList<MetadataReference> DbContextAssemblyReferences)> GetDbContextsUnderTest()
    {
        yield break;
    }

    protected ReferenceAssemblies GetReferenceAssemblies()
    {
#if NET8_0
        var refAssemblies = ReferenceAssemblies.Net.Net80.WithPackages([
            new("Microsoft.AspNetCore.App.Ref", "8.0.0")
            ]);
#elif NET10_0
        var refAssemblies = new Microsoft.CodeAnalysis.Testing.ReferenceAssemblies(
                    "net10.0",
                    new Microsoft.CodeAnalysis.Testing.PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "10.0.0"),
                    Path.Combine("ref", "net10.0"))
                .WithPackages([
                    new Microsoft.CodeAnalysis.Testing.PackageIdentity("Microsoft.AspNetCore.App.Ref", "10.0.0"),
                    //new Microsoft.CodeAnalysis.Testing.PackageIdentity("Microsoft.EntityFrameworkCore", "10.0.0")
                ]);
#else
                throw new PlatformNotSupportedException("Please define reference assemblies for your platform!");
#endif

        return refAssemblies;
    }

    protected virtual IEnumerable<MetadataReference> GetMigrationAssemblyReferences()
    {
        yield break;
    }

    private IEnumerable<MetadataReference> getDbContextsAssembliesUnderTest()
    {
        var referenceAssemblies = GetReferenceAssemblies().ResolveAsync(LanguageNames.CSharp, default).GetAwaiter().GetResult();

        var dbContextsUnderTest = GetDbContextsUnderTest();

        foreach (var dbContextInfo in dbContextsUnderTest)
        {
            var syntaxTrees = dbContextInfo.Sources.Select(source => CSharpSyntaxTree.ParseText(source));

            var compilation = CSharpCompilation.Create(
                assemblyName: dbContextInfo.AssemblyName,
                syntaxTrees: syntaxTrees,
                references:
                [
                    .. referenceAssemblies,
                    MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Basilisque.DataAccess.EntityFramework.Relational.Model.BaseDbContext<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Basilisque.DataAccess.EntityFramework.Base.Model.BaseDbContext<>).Assembly.Location),
                    .. dbContextInfo.DbContextAssemblyReferences
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                throw new Exception("Compilation failed: " +
                    string.Join("\n", result.Diagnostics));
            }

            ms.Position = 0;

            yield return MetadataReference.CreateFromImage(ms.ToArray());
        }
    }

    protected virtual IEnumerable<(string filename, string content)> GetAnalyzerConfigFiles()
    {
        yield return ("/.editorconfig",
            """
            is_global = true
            build_property.BAS_DA_EF_IsMigrationAssembly = true
            build_property.BAS_DA_EF_DesignTimeDbContextFactories = ,global::Basilisque.DataAccess.EntityFramework.SqlServer.Design.SqlServerBaseDesignTimeDbContextFactory,global::Basilisque.DataAccess.EntityFramework.SQLite.Design.SQLiteBaseDesignTimeDbContextFactory,global::Basilisque.DataAccess.EntityFramework.PostgreSQL.Design.PostgreSQLBaseDesignTimeDbContextFactory
            """);
    }

    protected virtual IEnumerable<(string key, Microsoft.CodeAnalysis.ReportDiagnostic value)> GetDiagnosticOptions()
    {
        //we can return diagnostic options like this:
        //yield return ("CS1591", Microsoft.CodeAnalysis.ReportDiagnostic.Suppress);
        //yield return ("CS159?", Microsoft.CodeAnalysis.ReportDiagnostic.???);

        yield break;
    }

    protected virtual IEnumerable<DiagnosticResult> GetExpectedDiagnostics(string assemblyName, string assemblyVersion)
    {
        //we can return expected diagnostics like this:
        //yield return new Microsoft.CodeAnalysis.Testing.DiagnosticResult("CS1591", Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        yield break;
    }

    protected virtual void AddSourcesUnderTest(SourceFileList sources)
    { /* for overriding purposes only */ }

    protected virtual IEnumerable<(string Name, string SourceText)> GetExpectedAttributeSources(IReadOnlyDictionary<string, (string CompilationName, string Source)> supportedAttributes)
    {
        return supportedAttributes.Values;
    }

    protected abstract IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources();

    private List<(string Name, string SourceText)> getExpectedSources()
    {
        var result = new List<(string Name, string SourceText)>();

        addStaticSources(result);

        result.AddRange(GetExpectedDbContextFactorySources());

        return result;
    }

    private void addStaticSources(List<(string Name, string SourceText)> sources)
    {
        sources.AddRange(GetExpectedAttributeSources(DesignTimeServicesAttributeGeneratorData.SupportedDesignTimeServicesAttributes));
    }
}
