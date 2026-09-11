/*
   Copyright 2026 Alexander Stärk

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

using Basilisque.DataAccess.EntityFramework.Base.Model;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Model;

public class GuidVersion7ValueGeneratorTests
{
    [Test]
    public async Task GeneratesTemporaryValues_is_false()
    {
        var sut = new GuidVersion7ValueGenerator();

        await Assert.That(sut.GeneratesTemporaryValues).IsFalse();
    }

    [Test]
    public async Task Next_returns_non_empty_guid_v7()
    {
        var sut = new GuidVersion7ValueGenerator();

        var value = sut.Next(null!);

        await Assert.That(value).IsNotEqualTo(Guid.Empty);
        await Assert.That(value.ToString("D")[14]).IsEqualTo('7');
    }
}
