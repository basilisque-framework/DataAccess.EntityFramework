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

using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators;

internal class CommonGeneratorSelectors
{
    private static readonly char[] _dbContextFactoriesSeparators = new char[] { ',', ';' };

    internal static BuildPropertyInfo BuildPropertiesSelector(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool isMigrationAssembly;
        if (provider.GlobalOptions.TryGetValue("build_property.BAS_DA_EF_IsMigrationAssembly", out string? isMigrationAssemblyString) && !string.IsNullOrWhiteSpace(isMigrationAssemblyString))
        {
            if (!bool.TryParse(isMigrationAssemblyString, out isMigrationAssembly))
                isMigrationAssembly = false;
        }
        else
            isMigrationAssembly = false;

        string[]? dbContextFactories;
        if (provider.GlobalOptions.TryGetValue("build_property.BAS_DA_EF_DesignTimeDbContextFactories", out string? dbContextFactoriesString) && !string.IsNullOrWhiteSpace(dbContextFactoriesString))
        {
            dbContextFactories = dbContextFactoriesString.Split(_dbContextFactoriesSeparators, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
            dbContextFactories = null;

        return new BuildPropertyInfo(isMigrationAssembly, dbContextFactories);
    }
}
