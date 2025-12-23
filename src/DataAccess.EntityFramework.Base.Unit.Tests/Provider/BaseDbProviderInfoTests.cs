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

using Basilisque.DataAccess.EntityFramework.Base.Provider;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Provider;

public class BaseDbProviderInfoTests
{
    private class TestProviderInfo : BaseDbProviderInfo
    {
        public override string ProviderKey => "Sql";
        public override string ProviderName => "SQL Provider";
    }

    [Test]
    public async Task Properties_are_exposed_by_derived_class()
    {
        var info = new TestProviderInfo();
        await Assert.That(info.ProviderKey).IsEqualTo("Sql");
        await Assert.That(info.ProviderName).IsEqualTo("SQL Provider");
    }
}
