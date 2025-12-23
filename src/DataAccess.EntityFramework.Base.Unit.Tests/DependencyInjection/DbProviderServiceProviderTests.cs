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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.DependencyInjection;

public class DbProviderServiceProviderTests
{
    public interface IFoo { string Who { get; } }
    private class FooDefault : IFoo { public string Who => "default"; }
    private class FooSqlServer : IFoo { public string Who => "sqlserver"; }

    private static (IConfiguration, ServiceProvider) Build(string? providerName, bool registerKeyed, bool registerDefault)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = providerName })
            .Build();

        var services = new ServiceCollection();
        if (registerDefault)
            services.AddSingleton<IFoo, FooDefault>();
        if (registerKeyed)
            services.AddKeyedSingleton<IFoo>("SqlServer", new FooSqlServer());

        return (cfg, services.BuildServiceProvider());
    }

    [Test]
    public async Task GetRequiredService_prefers_keyed_by_provider_name()
    {
        var (cfg, sp) = Build("SqlServer", registerKeyed: true, registerDefault: true);
        var sut = new DbProviderServiceProvider(cfg, sp);

        var foo = sut.GetRequiredService<IFoo>();

        await Assert.That(foo.Who).IsEqualTo("sqlserver");
    }

    [Test]
    public async Task Falls_back_to_default_service_when_no_keyed_match()
    {
        var (cfg, sp) = Build("Other", registerKeyed: false, registerDefault: true);
        var sut = new DbProviderServiceProvider(cfg, sp);

        var foo = sut.GetRequiredService<IFoo>();

        await Assert.That(foo.Who).IsEqualTo("default");
    }

    [Test]
    public async Task Throws_when_no_provider_configured_and_no_default()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var sp = new ServiceCollection().AddSingleton<IFoo, FooDefault>().BuildServiceProvider();
        var sut = new DbProviderServiceProvider(cfg, sp);

        await Assert.That(() => sut.GetRequiredService<IFoo>()).Throws<InvalidOperationException>();
    }
}
