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

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

/// <summary>
/// Defines soft deletion using only a nullable deletion time.
/// </summary>
public interface ISoftDeleteTimestamp
{
    /// <summary>
    /// Gets or sets the UTC deletion time, or null when not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets whether the entity has been deleted. This convenience property is not persisted.
    /// </summary>
    bool IsDeleted => DeletedAt.HasValue;
}
