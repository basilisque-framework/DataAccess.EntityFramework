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
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

internal sealed class UserStampDispatcher : IStampHandler
{
    private static readonly ConditionalWeakTable<IModel, Type[]> _handlerTypes = new();
    private readonly IServiceProvider _serviceProvider;

    public UserStampDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
    }

    public void UpdateStampProperties(DbContext context, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handlerTypes = _handlerTypes.GetValue(context.Model, static model => model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.GetStampPropertyKind() is StampPropertyKind.CreatedBy or StampPropertyKind.ModifiedBy)
            .Select(property => property.ClrType)
            .Distinct()
            .Select(keyType => typeof(IUserStampHandler<>).MakeGenericType(keyType))
            .ToArray());

        foreach (var handlerType in handlerTypes)
        {
            var handler = (IStampHandler)_serviceProvider.GetRequiredService(handlerType);
            handler.UpdateStampProperties(context, timestamp);
        }
    }
}
