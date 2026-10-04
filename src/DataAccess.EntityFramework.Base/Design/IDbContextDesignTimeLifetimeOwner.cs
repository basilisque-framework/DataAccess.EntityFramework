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

namespace Basilisque.DataAccess.EntityFramework.Base.Design;

/// <summary>
/// Accepts ownership of the services used to create a design-time database context.
/// </summary>
public interface IDbContextDesignTimeLifetimeOwner
{
    /// <summary>
    /// Attaches the lifetime to a live context. A second attachment must be rejected.
    /// </summary>
    /// <remarks>
    /// The context must dispose the lifetime in both its synchronous and asynchronous
    /// disposal paths, even if context disposal fails. Ownership transfers only when
    /// this method returns successfully.
    /// </remarks>
    /// <param name="lifetime">The service lifetime to own until context disposal.</param>
    void SetDesignTimeServiceLifetime(IDesignTimeServiceLifetime lifetime);
}
