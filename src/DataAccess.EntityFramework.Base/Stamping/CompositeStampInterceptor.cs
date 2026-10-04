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

using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Invokes registered stamp handlers before Entity Framework Core saves changes.
/// </summary>
[RegisterServiceScoped]
public sealed class CompositeStampInterceptor : SaveChangesInterceptor
{
    private readonly IEnumerable<IStampHandler> _handlers;

    /// <summary>
    /// Creates a new composite stamp interceptor.
    /// </summary>
    /// <param name="handlers">The stamp handlers to invoke.</param>
    public CompositeStampInterceptor(IEnumerable<IStampHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        updateStampProperties(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        updateStampProperties(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void updateStampProperties(DbContext? context)
    {
        if (context is null)
            return;

        var timestamp = DateTimeOffset.UtcNow;
        SoftDeleteProcessor.Apply(context, timestamp);
        if (StampSuppressor.IsSuppressed)
            return;

        foreach (var handler in _handlers)
            handler.UpdateStampProperties(context, timestamp);
    }
}
