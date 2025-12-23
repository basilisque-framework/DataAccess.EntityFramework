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

using Basilisque.DataAccess.EntityFramework.Relational.Design;
using Basilisque.DataAccess.EntityFramework.Relational.Model.DbScripts;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Relational.Unit.Tests.Design;

public class RelationalDesignTimeServicesTests
{
    [Test]
    public async Task ConfigureDesignTimeServices_replaces_model_differ_with_db_scripts_variant()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMigrationsModelDiffer, MigrationsModelDifferStub>();

        var svc = new RelationalDesignTimeServices();
        svc.ConfigureDesignTimeServices(services);

        // Assert on the descriptor directly to avoid needing EF internal DI services
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IMigrationsModelDiffer));
        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor!.ImplementationType).IsEqualTo(typeof(DbScriptsMigrationsModelDiffer));
    }

    private class MigrationsModelDifferStub : IMigrationsModelDiffer
    {
        public IReadOnlyList<Microsoft.EntityFrameworkCore.Migrations.Operations.MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target) => [];
        public bool HasDifferences(IRelationalModel? source, IRelationalModel? target) => false;
    }
}
