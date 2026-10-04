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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Runtime.CompilerServices;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

internal sealed class UserStampDispatcher : IStampHandler
{
    private static readonly ConditionalWeakTable<IModel, Type[]> _keyTypes = new();
    private readonly Dictionary<Type, IUserStampHandler> _handlers = new();

    public UserStampDispatcher(IEnumerable<IUserStampHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var handler in handlers)
            _handlers[handler.UserKeyType] = handler;
    }

    public void UpdateStampProperties(DbContext context, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(context);

        var keyTypes = _keyTypes.GetValue(context.Model, static model => model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.GetStampPropertyKind() is StampPropertyKind.CreatedBy or StampPropertyKind.ModifiedBy)
            .Select(property => property.ClrType)
            .Distinct()
            .ToArray());

        foreach (var keyType in keyTypes)
        {
            if (!_handlers.ContainsKey(keyType))
                throw new InvalidOperationException($"No user stamp handler is registered for key type '{keyType.FullName}'. Register a scoped {nameof(IUserStampHandler)} with {nameof(IUserStampHandler.UserKeyType)} equal to this type, for example a closed UserStampHandler<TKey>, after the generated registration chain.");
        }

        foreach (var keyType in keyTypes)
            _handlers[keyType].UpdateStampProperties(context, timestamp);
    }
}
