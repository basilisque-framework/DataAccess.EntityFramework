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
using Basilisque.DataAccess.EntityFramework.SqlServer.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Unit.Tests.Connection;

public class SqlServerConnectionStringBuilderTests
{
    [Test]
    public async Task Selects_nested_context_and_provider_specific_section_and_uses_sqlclient_builder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionStrings:DummyContext:SqlServer:ConnectionString"] = "Server=.;Database=Dummy;",
                ["Database:ConnectionStrings:DummyContext:SqlServer:User Id"] = "sa",
                ["Database:ConnectionStrings:DummyContext:SqlServer:Password"] = "pw"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();
        new SqlServerBaseDesignTimeDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>()
            .ConfigureProviderServices(services, args: [], isChildFactory: false);

        var sp = services.BuildServiceProvider();
        var sut = sp.GetKeyedService<IConnectionStringBuilder>("SqlServer")!;

        var result = sut.GetConnectionString("DummyContext", "SqlServer");

        var lower = result.ToLowerInvariant();
        await Assert.That(lower).Contains("initial catalog=dummy");
        await Assert.That(lower).Contains("user id=sa");
        await Assert.That(lower).Contains("password=pw");
    }
}
