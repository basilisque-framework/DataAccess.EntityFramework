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

using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.Base.Design;

internal sealed class DesignTimeServiceLifetime(ServiceProvider provider, AsyncServiceScope scope) : IDesignTimeServiceLifetime
{
    private int _isDisposed;

    public IServiceProvider ServiceProvider => scope.ServiceProvider;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        try
        {
            scope.Dispose();
        }
        finally
        {
            provider.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        try
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await provider.DisposeAsync().ConfigureAwait(false);
        }
    }
}
