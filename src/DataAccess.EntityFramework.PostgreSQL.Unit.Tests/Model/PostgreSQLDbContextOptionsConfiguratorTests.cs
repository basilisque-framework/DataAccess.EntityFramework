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

using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Unit.Tests.Model;

public class PostgreSQLDbContextOptionsConfiguratorTests
{
    private class DummyContext : BaseDbContext<DummyContext>
    {
        public DummyContext(IDbProviderServiceProvider sp) : base(sp) { }
    }

    [Test]
    public async Task Configure_sets_npgsql_provider_with_connection_string()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionStrings:DummyContext:PostgreSQL:ConnectionString"] = "Host=localhost;Database=Dummy;Username=pg;Password=pw"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();
        new PostgreSQLBaseDesignTimeDbContextFactory<DbContext>()
            .ConfigureProviderServices(services, args: [], isChildFactory: false);

        var sp = services.BuildServiceProvider();
        var cfg = sp.GetKeyedService<IDbContextOptionsConfigurator>("PostgreSQL")!;
        var ctx = new DummyContext(sp.GetRequiredService<IDbProviderServiceProvider>());

        var ob = new DbContextOptionsBuilder();
        cfg.Configure(ctx, ob);

        // Verify provider configured without asserting on internal types
        var hasNpgsql = ob.Options.Extensions.Any(e => e.GetType().Namespace?.Contains("Npgsql", System.StringComparison.OrdinalIgnoreCase) == true);
        await Assert.That(hasNpgsql).IsTrue();
    }
}
