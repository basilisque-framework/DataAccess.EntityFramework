using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.MigrationAssemblyProviderGenerator;

/// <summary>Tests migration ownership instead of assuming that every referenced context belongs to an assembly.</summary>
public sealed class MigrationMappingTests
{
    private const string ContextSource = """
        using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
        using Basilisque.DataAccess.EntityFramework.Base.Model;
        namespace MappingTest;
        public sealed class OwnedContext : BaseDbContext<OwnedContext> { public OwnedContext(IDbProviderServiceProvider services) : base(services) { } }
        public sealed class OtherContext : BaseDbContext<OtherContext> { public OtherContext(IDbProviderServiceProvider services) : base(services) { } }
        """;
    private const string SnapshotSource = """
        namespace MappingTest;
        [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(OwnedContext))]
        public sealed class OwnedSnapshot : Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot
        {
            protected override void BuildModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) { }
        }
        """;
    private readonly MetadataReference[] _references;

    /// <summary>Collects compiler references for isolated in-memory generator runs.</summary>
    public MigrationMappingTests()
    {
        var platformPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        _references = platformPaths.Concat([
            typeof(BaseDbContext<>).Assembly.Location,
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location,
            typeof(Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot).Assembly.Location])
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    [Test]
    public async Task Generate_ShouldMapOnlyOwnedContext_WhenSnapshotIdentifiesOneOfSeveralContexts()
    {
        // Arrange: two source contexts, but only one belongs to this migration assembly.
        var compilation = createCompilation(ContextSource, SnapshotSource);
        // Act
        var result = runGenerator(compilation);
        // Assert
        var mapping = getMappingSource(result);
        await Assert.That(mapping.Contains("typeof(global::MappingTest.OwnedContext)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(mapping.Contains("typeof(global::MappingTest.OtherContext)", StringComparison.Ordinal)).IsFalse();
        await Assert.That(mapping.Contains("databaseProvider == \"SQLite\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(mapping.Contains("databaseProvider == \"SqlServer\"", StringComparison.Ordinal)).IsFalse();
        await Assert.That(result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error)).IsFalse();
    }

    [Test]
    public async Task Generate_ShouldMapSingleReferencedContext_BeforeFirstMigrationExists()
    {
        // Arrange
        var referenced = createContextReference(ContextSource.Replace("public sealed class OtherContext : BaseDbContext<OtherContext> { public OtherContext(IDbProviderServiceProvider services) : base(services) { } }", ""));
        var compilation = createCompilation().AddReferences(referenced);
        // Act
        var result = runGenerator(compilation);
        // Assert
        await Assert.That(getMappingSource(result).Contains("typeof(global::MappingTest.OwnedContext)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Diagnostics.Any(item => item.Id == "BAS_EF001")).IsFalse();
    }

    [Test]
    public async Task Generate_ShouldReportAmbiguousContext_WhenEmptyAssemblyReferencesSeveralContexts()
    {
        // Arrange
        var compilation = createCompilation().AddReferences(createContextReference(ContextSource));
        // Act
        var result = runGenerator(compilation);
        // Assert
        await Assert.That(result.Diagnostics.Count(item => item.Id == "BAS_EF001")).IsEqualTo(1);
        await Assert.That(getMappingSource(result).Contains("dbContextType ==", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Generate_ShouldUseLocalSnapshotOwnership_WhenOtherContextsAreReferenced()
    {
        // Arrange
        var compilation = createCompilation(SnapshotSource).AddReferences(createContextReference(ContextSource));
        // Act
        var result = runGenerator(compilation);
        // Assert
        var mapping = getMappingSource(result);
        await Assert.That(mapping.Contains("typeof(global::MappingTest.OwnedContext)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(mapping.Contains("typeof(global::MappingTest.OtherContext)", StringComparison.Ordinal)).IsFalse();
        await Assert.That(result.Diagnostics.Any(item => item.Id == "BAS_EF001")).IsFalse();
    }

    [Test]
    public async Task Generate_ShouldTargetFactoryAssembly_ForContextWithoutItsFirstSnapshot()
    {
        // Arrange: only OwnedContext has migrations; OtherContext still needs an initial migration.
        var compilation = createCompilation(ContextSource, SnapshotSource);
        // Act
        var result = runGenerator(compilation);
        // Assert: the design-time factory explicitly selects its own assembly for OtherContext.
        var factory = result.Results.Single().GeneratedSources.Single(source => source.HintName.Contains("OtherContextDesignTimeFactory", StringComparison.Ordinal)).SourceText.ToString();
        await Assert.That(factory.Contains("AddMigrationAssembly<global::MappingTest.OtherContext>", StringComparison.Ordinal)).IsTrue();
        await Assert.That(factory.Contains("typeof(OtherContextDesignTimeFactory).Assembly", StringComparison.Ordinal)).IsTrue();
    }

    private CSharpCompilation createCompilation(params string[] sources) => CSharpCompilation.Create(
        "MappingAssembly", sources.Select(source => CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))),
        _references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private MetadataReference createContextReference(string source)
    {
        using var stream = new MemoryStream();
        var result = createCompilation(source).WithAssemblyName("ContextAssembly").Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static GeneratorDriverRunResult runGenerator(CSharpCompilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new DataAccessEntityFrameworkGenerator().AsSourceGenerator()],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), optionsProvider: new GeneratorOptions());
        var result = driver.RunGenerators(compilation).GetRunResult();
        if (result.Results.Any(item => item.Exception is not null))
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        return result;
    }

    private static string getMappingSource(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Single(source => source.HintName.EndsWith("Basilisque_DataAccess_EntityFramework_MigrationAssemblyProvider.g.cs", StringComparison.Ordinal)).SourceText.ToString();

    private sealed class GeneratorOptions : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options();
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }
    private sealed class Options : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = key switch
            {
                "build_property.BAS_DA_EF_IsMigrationAssembly" => "true",
                "build_property.BAS_DA_EF_DatabaseProviders" => "SQLite",
                "build_property.BAS_DA_EF_DesignTimeDbContextFactories" => "global::Basilisque.DataAccess.EntityFramework.SQLite.Design.SQLiteBaseDesignTimeDbContextFactory",
                "build_property.RootNamespace" => "MappingTest",
                "build_property.AssemblyName" => "MappingAssembly",
                _ => ""
            };
            return value.Length > 0;
        }
    }
}
