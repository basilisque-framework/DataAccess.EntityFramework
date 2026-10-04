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

using Basilisque.Core.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Applies creator and modifier keys of type <typeparamref name="TKey"/> to configured entities.
/// </summary>
/// <typeparam name="TKey">The type of the user key.</typeparam>
public sealed class UserStampHandler<TKey> : IUserStampHandler<TKey>
    where TKey : notnull
{
    private readonly IUserContext<TKey> _userContext;

    /// <summary>
    /// Creates a new user stamp handler.
    /// </summary>
    /// <param name="userContext">The context used to retrieve the current user key.</param>
    public UserStampHandler(IUserContext<TKey> userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        _userContext = userContext;
    }

    /// <inheritdoc />
    public void UpdateStampProperties(DbContext context, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_userContext.TryGetCurrentUserId(out var userId))
            return;

        foreach (var entry in context.ChangeTracker.Entries())
            updateEntry(entry, userId);
    }

    private static void updateEntry(EntityEntry entry, TKey userId)
    {
        if (entry.State == EntityState.Added)
        {
            setUser(entry, StampPropertyKind.CreatedBy, userId);
            setUser(entry, StampPropertyKind.ModifiedBy, userId);
            return;
        }

        if (entry.State == EntityState.Modified && entry.HasMeaningfulModification())
            setUser(entry, StampPropertyKind.ModifiedBy, userId);
    }

    private static void setUser(EntityEntry entry, StampPropertyKind propertyKind, TKey userId)
    {
        foreach (var property in entry.Properties.Where(property => isUserProperty(property, propertyKind)))
            property.CurrentValue = userId;
    }

    private static bool isUserProperty(PropertyEntry property, StampPropertyKind propertyKind)
    {
        return property.Metadata.GetStampPropertyKind() == propertyKind && property.Metadata.ClrType == typeof(TKey);
    }
}
