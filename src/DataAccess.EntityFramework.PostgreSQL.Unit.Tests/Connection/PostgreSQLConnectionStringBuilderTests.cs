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

using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Unit.Tests.Connection;

public class PostgreSQLConnectionStringBuilderTests
{
    [Test]
    public async Task Selects_nested_context_and_provider_specific_section_and_uses_npgsql_builder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionStrings:MyContext:PostgreSQL:ConnectionString"] = "Host=localhost;Database=MyDb;",
                ["Database:ConnectionStrings:MyContext:PostgreSQL:Username"] = "postgres",
                ["Database:ConnectionStrings:MyContext:PostgreSQL:Password"] = "secret"
            })
            .Build();

        var services = new ServiceCollection();
        // Register required infra
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();
        // Register provider-specific services via the public factory
        new PostgreSQLBaseDesignTimeDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>()
            .ConfigureProviderServices(services, args: [], isChildFactory: false);

        var sp = services.BuildServiceProvider();
        var sut = sp.GetKeyedService<IConnectionStringBuilder>("PostgreSQL")!;

        var result = sut.GetConnectionString("MyContext", "PostgreSQL");

        // NpgsqlConnectionStringBuilder normalizes keys and order; assert by content, case-insensitive
        var lower = result.ToLowerInvariant();
        await Assert.That(lower).Contains("host=localhost");
        await Assert.That(lower).Contains("database=mydb");
        await Assert.That(lower).Contains("username=postgres");
        await Assert.That(lower).Contains("password=secret");
    }
}
