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
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.PostgreSQL.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Unit.Tests.Design;

public class PostgreSQLBaseDesignTimeDbContextFactoryTests
{
    [Test]
    public async Task ConfigureProviderServices_registers_keyed_services()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();

        var factory = new PostgreSQLBaseDesignTimeDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>();
        factory.ConfigureProviderServices(services, args: [], isChildFactory: false);

        var sp = services.BuildServiceProvider();

        var info = sp.GetKeyedService<IDbProviderInfo>("PostgreSQL");
        var cfg = sp.GetKeyedService<IDbContextOptionsConfigurator>("PostgreSQL");
        var csb = sp.GetKeyedService<IConnectionStringBuilder>("PostgreSQL");

        await Assert.That(info).IsNotNull();
        await Assert.That(cfg).IsNotNull();
        await Assert.That(csb).IsNotNull();
    }
}
