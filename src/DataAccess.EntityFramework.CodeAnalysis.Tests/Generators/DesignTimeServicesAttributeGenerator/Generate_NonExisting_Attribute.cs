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

using Microsoft.CodeAnalysis.Testing;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Unit.Tests.Generators.DesignTimeServicesAttributeGenerator;

[InheritsTests]
[Category(DesignTimeServicesAttributeGeneratorCategory)]
public class DoNotGenerate_WhenDisabled : BaseDataAccessEntityFrameworkGeneratorTest
{
    protected override void AddSourcesUnderTest(SourceFileList sources)
    {
        // nothing to add here because this test checks the case that no attribute is defined yet
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedDbContextFactorySources()
    {
        yield break;
    }

    protected override IEnumerable<(string Name, string SourceText)> GetExpectedAttributeSources(IReadOnlyDictionary<string, (string CompilationName, string Source)> supportedAttributes)
    {
        // the expected attribute source to generate is defined in the base class

        return base.GetExpectedAttributeSources(supportedAttributes);
    }
}

