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

internal sealed class DesignTimeServiceLifetime : IDesignTimeServiceLifetime
{
    private readonly ServiceProvider _provider;
    private readonly AsyncServiceScope _scope;
    private int _isDisposed;

    public DesignTimeServiceLifetime(ServiceProvider provider, AsyncServiceScope scope)
    {
        _provider = provider;
        _scope = scope;
    }

    public IServiceProvider ServiceProvider => _scope.ServiceProvider;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        try
        {
            _scope.Dispose();
        }
        finally
        {
            _provider.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        try
        {
            await _scope.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
        }
    }
}