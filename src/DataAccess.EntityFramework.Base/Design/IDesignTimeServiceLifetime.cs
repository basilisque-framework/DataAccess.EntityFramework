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
/// Owns the service scope and provider used by a design-time database context.
/// </summary>
/// <remarks>
/// Dispose this lifetime after disposing the context. Detach it from the context
/// before disposal because scope disposal can dispose the context again.
/// </remarks>
public interface IDesignTimeServiceLifetime : IDisposable, IAsyncDisposable
{
}
