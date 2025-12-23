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

using Basilisque.DataAccess.EntityFramework.Base.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Design;

public class BaseDesignTimeDbContextFactoryTests
{
    private class FakeDesignContext : DbContext, IDbContextDesignSupport
    {
        public bool IsDesignTime { get; set; }
        public FakeDesignContext() : base(new DbContextOptionsBuilder<FakeDesignContext>().Options) { }
    }

    private class TestFactory : BaseDesignTimeDbContextFactory<FakeDesignContext>
    {
        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddTransient<FakeDesignContext>();
        }
    }

    [Test]
    public async Task CreateDbContext_sets_IsDesignTime_true()
    {
        var factory = new TestFactory();

        var ctx = factory.CreateDbContext([]);

        await Assert.That(ctx.IsDesignTime).IsTrue();
    }
}
