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

using Basilisque.DataAccess.EntityFramework.Base.Data;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Data;

public class BaseDbInitializerTests
{
    internal class FakeDbContext : DbContext, IInitializableDbContext
    {
        public bool WasAlreadyMigrated { get; set; }
        public FakeDbContext() : base(new DbContextOptionsBuilder<FakeDbContext>().Options) { }
    }

    internal class TestInitializer : BaseDbInitializer<FakeDbContext>
    {
        public int ExecuteCount { get; private set; }
        public TestInitializer(FakeDbContext ctx) : base(ctx) { }
        protected override void ExecuteMigration() { ExecuteCount++; }
    }

    [Test]
    public async Task MigrateDatabase_runs_once_and_sets_flag()
    {
        var ctx = new FakeDbContext();
        var init = new TestInitializer(ctx);

        init.MigrateDatabase();
        await Assert.That(init.ExecuteCount).IsEqualTo(1);
        await Assert.That(ctx.WasAlreadyMigrated).IsTrue();

        init.MigrateDatabase();
        await Assert.That(init.ExecuteCount).IsEqualTo(1);
    }
}
