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

namespace Basilisque.DataAccess.EntityFramework.Base.Model;

/// <summary>
/// Implementing this interface provides support for DbContexts to be initialized.
/// </summary>
public interface IInitializableDbContext
{
    /// <summary>
    /// Indicates whether the current DbContext was already migrated.
    /// </summary>
    /// <remarks>Internally saves this state in a <see langword="static"/> field.</remarks>
    /// <returns>A boolean value indicating whether the current DbContext was already migrated.</returns>
    bool WasAlreadyMigrated { get; set; }
}
