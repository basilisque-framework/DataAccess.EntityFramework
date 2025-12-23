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
using Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Model.DbScripts;

public class ModelDbScriptsConventionTests
{
    private class DummyContext : BaseDbContext<DummyContext>
    {
        public DummyContext(IDbProviderServiceProvider sp) : base(sp) { }
    }

    private class ExposedConvention : ModelDbScriptsConvention<DummyContext>
    {
        public ExposedConvention(BaseDbContext<DummyContext> ctx) : base(ctx) { }
        public string ExposedReplace(string content, string name, IConventionModelBuilder mb)
            => base.ReplacePlaceholders(content, name, mb);
    }

    private class ProviderServices : IDbProviderServiceProvider
    {
        public string DatabaseConfigurationSectionName => "Database";
        public T GetRequiredService<T>() where T : notnull => throw new NotSupportedException();
        public T? GetService<T>() => default;
    }

    [Test]
    public async Task ReplacePlaceholders_replaces_script_name()
    {
        var ctx = new DummyContext(new ProviderServices());
        var conv = new ExposedConvention(ctx);

        var result = conv.ExposedReplace("CREATE PROC {{ScriptName}} AS SELECT 1;", "MyProc", null!);

        await Assert.That(result).IsEqualTo("CREATE PROC MyProc AS SELECT 1;");
    }
}
