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
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Model;

public class BaseDbContextOptionsConfiguratorTests
{
    private class TestConfigurator : BaseDbContextOptionsConfigurator
    {
        public bool Called { get; private set; }
        protected override void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        {
            Called = true;
        }
    }

    private class FakeProviderServices : IDbProviderServiceProvider
    {
        public string DatabaseConfigurationSectionName => "Database";
        public T GetRequiredService<T>() where T : notnull => throw new NotSupportedException();
        public T? GetService<T>() => default;
    }

    private class DummyContext : BaseDbContext<DummyContext>
    {
        public DummyContext(IDbProviderServiceProvider sp) : base(sp) { }
    }

    [Test]
    public async Task Configure_delegates_to_OnConfigure()
    {
        var cfg = new TestConfigurator();
        var ctx = new DummyContext(new FakeProviderServices());

        var ob = new DbContextOptionsBuilder();
        cfg.Configure(ctx, ob);

        await Assert.That(cfg.Called).IsTrue();
    }
}
