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

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Updates configured entity stamp properties before changes are saved.
/// </summary>
public interface IStampHandler
{
    /// <summary>
    /// Updates stamp properties tracked by the specified database context.
    /// </summary>
    /// <param name="context">The database context that is saving changes.</param>
    /// <param name="timestamp">The common timestamp for the current save operation.</param>
    void UpdateStampProperties(DbContext context, DateTimeOffset timestamp);
}
