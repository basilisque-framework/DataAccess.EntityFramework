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

using Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators;
using Microsoft.CodeAnalysis;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.DesignTimeDbContextFactoryGenerator;

[InheritsTests]
[Category(DesignTimeDbContextFactoryGeneratorCategory)]
public class Generate_1_DbContext_All_Providers : BaseDataAccessEntityFrameworkGeneratorTest
{
    protected override IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources()
    {
        yield return (
                Name: "DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext.MyDbContextDesignTimeFactory.g.cs",
                SourceText: @$"{CommonGeneratorData.GeneratedFileSharedHeaderWithNullable}
namespace DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext;

/// <inheritdoc />
{CommonGeneratorData.GeneratedClassSharedAttributesNotIndented}
public partial class MyDbContextDesignTimeFactory : global::Basilisque.DataAccess.EntityFramework.Base.Design.BaseDesignTimeDbContextFactory<DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext.MyDbContext>
{{
    private global::Basilisque.DataAccess.EntityFramework.PostgreSQL.Design.PostgreSQLBaseDesignTimeDbContextFactory<DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext.MyDbContext> _designTimeFactory1 = new();
    private global::Basilisque.DataAccess.EntityFramework.SQLite.Design.SQLiteBaseDesignTimeDbContextFactory<DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext.MyDbContext> _designTimeFactory2 = new();
    private global::Basilisque.DataAccess.EntityFramework.SqlServer.Design.SqlServerBaseDesignTimeDbContextFactory<DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext.MyDbContext> _designTimeFactory3 = new();
    
    /// <inheritdoc />
    public sealed override void ConfigureProviderServices(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, string[] args, bool isChildFactory)
    {{
        _designTimeFactory1.ConfigureProviderServices(services, args, isChildFactory: true);
        _designTimeFactory2.ConfigureProviderServices(services, args, isChildFactory: true);
        _designTimeFactory3.ConfigureProviderServices(services, args, isChildFactory: true);
    }}
}}

#nullable restore");
    }

    protected override IEnumerable<(string AssemblyName, IReadOnlyList<string> Sources, IReadOnlyList<MetadataReference> DbContextAssemblyReferences)> GetDbContextsUnderTest()
    {
        yield return ("DbContextAssembly", [@"
#nullable enable

using Basilisque.DataAccess.EntityFramework.Relational.Model;

namespace DbContextAssembly.DataAccess.EFG.TestObjects.Generate_1_DbContext;

/// <summary>
/// Test DbContext
/// </summary>
public class MyDbContext : BaseDbContext<MyDbContext>
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
        return ReferenceAssemblies_AllBaseDesignTimeDbContextFactories;
    }
}

