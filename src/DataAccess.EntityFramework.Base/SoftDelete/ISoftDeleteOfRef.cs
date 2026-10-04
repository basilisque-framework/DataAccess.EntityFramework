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
/// Defines soft deletion with a nullable deletion time and reference-type user key.
/// </summary>
/// <typeparam name="TKey">The user-key type.</typeparam>
public interface ISoftDeleteOfRef<TKey> : ISoftDeleteTimestamp
    where TKey : class
{
    /// <summary>
    /// Gets or sets the deleting user's key, or null when absent or not deleted.
    /// </summary>
    TKey? DeletedBy { get; set; }
}
