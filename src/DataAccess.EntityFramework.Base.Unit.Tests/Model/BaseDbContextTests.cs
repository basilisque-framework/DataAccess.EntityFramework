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
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Model;

public class BaseDbContextTests
{
    private class FakeProviderInfo : IDbProviderInfo
    {
        public required string ProviderKey { get; init; }
        public required string ProviderName { get; init; }
    }

    private class FlagConfigurator : IDbContextOptionsConfigurator
    {
        public bool WasCalled { get; private set; }
        public void Configure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
            where TDbContext : BaseDbContext<TDbContext>
        {
            WasCalled = true;
        }
    }

    private class FakeProviderServices : IDbProviderServiceProvider
    {
        public string DatabaseConfigurationSectionName => "Database";
        public required IDbProviderInfo ProviderInfo { get; init; }
        public required IDbContextOptionsConfigurator Configurator { get; init; }

        public T GetRequiredService<T>() where T : notnull
        {
            if (typeof(T) == typeof(IDbProviderInfo)) return (T)ProviderInfo;
            if (typeof(T) == typeof(IDbContextOptionsConfigurator)) return (T)Configurator;
            throw new NotSupportedException();
        }
        public T? GetService<T>() => default;
    }

    private class TestDbContext : BaseDbContext<TestDbContext>
    {
        public bool ConventionAdded { get; private set; }
        public TestDbContext(IDbProviderServiceProvider sp) : base(sp) { }

        public string? PublicGetProviderDirectoryNameForTesting() => base.GetProviderDirectoryName();

        public void InvokeOnConfiguring(DbContextOptionsBuilder b) => base.OnConfiguring(b);

        protected override void AddDesignTimeModelDbScriptsConvention(ModelConfigurationBuilder configurationBuilder)
        {
            ConventionAdded = true;
        }
    }

    [Test]
    public async Task OnConfiguring_invokes_configurator()
    {
        var providerInfo = Substitute.For<IDbProviderInfo>();
        providerInfo.ProviderKey.Returns("SqlServer");
        providerInfo.ProviderName.Returns("Microsoft SQL Server");

        var configurator = Substitute.For<IDbContextOptionsConfigurator>();

        var sp = Substitute.For<IDbProviderServiceProvider>();
        sp.DatabaseConfigurationSectionName.Returns("Database");
        sp.GetRequiredService<IDbProviderInfo>().Returns(providerInfo);
        sp.GetRequiredService<IDbContextOptionsConfigurator>().Returns(configurator);

        var ctx = new TestDbContext(sp);

        var ob = new DbContextOptionsBuilder();
        ctx.InvokeOnConfiguring(ob);

        configurator.Received(1).Configure(ctx, Arg.Any<DbContextOptionsBuilder>());
        await Assert.That(configurator.ReceivedCalls().Any()).IsTrue();
    }

    [Test]
    public async Task GetProviderDirectoryName_sanitizes_invalid_chars_and_trims()
    {
        var providerInfo = Substitute.For<IDbProviderInfo>();
        providerInfo.ProviderKey.Returns("  A<B>. ");
        providerInfo.ProviderName.Returns("X");

        var configurator = Substitute.For<IDbContextOptionsConfigurator>();

        var sp = Substitute.For<IDbProviderServiceProvider>();
        sp.DatabaseConfigurationSectionName.Returns("Database");
        sp.GetRequiredService<IDbProviderInfo>().Returns(providerInfo);
        sp.GetRequiredService<IDbContextOptionsConfigurator>().Returns(configurator);

        var ctx = new TestDbContext(sp);

        var dir = ctx.PublicGetProviderDirectoryNameForTesting();

        await Assert.That(dir).IsEqualTo("A<B>");
    }
}
