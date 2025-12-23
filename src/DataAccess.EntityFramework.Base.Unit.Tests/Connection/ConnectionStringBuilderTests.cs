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
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Connection;

public class ConnectionStringBuilderTests
{
    private class TestableConnectionStringBuilder : ConnectionStringBuilder
    {
        public TestableConnectionStringBuilder(IConfiguration configuration, IDbProviderServiceProvider provider)
            : base(configuration, provider) { }

        protected override DbConnectionStringBuilder? CreateConnectionStringBuilder(string? connectionString)
        {
            var b = new DbConnectionStringBuilder();
            if (!string.IsNullOrWhiteSpace(connectionString))
                b.ConnectionString = connectionString;
            return b;
        }
    }

    [Test]
    public async Task Returns_raw_value_when_no_builder_is_created()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=.;Database=RawDb;Trusted_Connection=True;"
            })
            .Build();

        var provider = Substitute.For<IDbProviderServiceProvider>();
        provider.DatabaseConfigurationSectionName.Returns("Database");

        var sut = new ConnectionStringBuilder(cfg, provider);

        var result = sut.GetConnectionString(contextName: null, providerKey: null);

        await Assert.That(result).IsEqualTo("Server=.;Database=RawDb;Trusted_Connection=True;");
    }

    [Test]
    public async Task Selects_nested_context_and_provider_specific_section()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionStrings:MyContext:SqlServer:ConnectionString"] = "Server=.;Database=MyDb;",
                ["Database:ConnectionStrings:MyContext:SqlServer:User Id"] = "sa",
                ["Database:ConnectionStrings:MyContext:SqlServer:Password"] = "secret"
            })
            .Build();

        var provider = Substitute.For<IDbProviderServiceProvider>();
        provider.DatabaseConfigurationSectionName.Returns("Database");
        var sut = new TestableConnectionStringBuilder(cfg, provider);

        var result = sut.GetConnectionString("MyContext", "SqlServer");

        var lower = result.ToLowerInvariant();
        await Assert.That(lower).Contains("database=mydb");
        await Assert.That(lower).Contains("user id=sa");
        await Assert.That(lower).Contains("password=secret");
    }

    [Test]
    public async Task Falls_back_to_root_database_section_when_no_connectionstrings_section()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=fallback;Database=FallbackDb;"
            })
            .Build();

        var provider = Substitute.For<IDbProviderServiceProvider>();
        provider.DatabaseConfigurationSectionName.Returns("Database");
        var sut = new TestableConnectionStringBuilder(cfg, provider);

        var result = sut.GetConnectionString(null, null);

        var lower = result.ToLowerInvariant();
        await Assert.That(lower).Contains("server=fallback");
        await Assert.That(lower).Contains("database=fallbackdb");
    }
}
