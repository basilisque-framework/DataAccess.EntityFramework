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
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.SqlServer.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Unit.Tests.Connection;

public class SqlServerDbProviderInfoTests
{
    [Test]
    public async Task ProviderInfo_is_resolvable_and_has_expected_values()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();
        new SqlServerBaseDesignTimeDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>()
            .ConfigureProviderServices(services, args: [], isChildFactory: false);

        var sp = services.BuildServiceProvider();
        var info = sp.GetKeyedService<IDbProviderInfo>("SqlServer");

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.ProviderKey).IsEqualTo("SqlServer");
        await Assert.That(info.ProviderName).Contains("SqlServer", System.StringComparison.OrdinalIgnoreCase);
    }
}
