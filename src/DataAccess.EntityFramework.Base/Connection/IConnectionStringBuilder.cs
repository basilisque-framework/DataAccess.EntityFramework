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

namespace Basilisque.DataAccess.EntityFramework.Base.Connection;

/// <summary>
/// Provides methods to build connection strings based on configuration settings.
/// </summary>
public interface IConnectionStringBuilder
{
    /// <summary>
    /// Reads connection settings from the configuration file and builds a connection string.
    /// </summary>
    /// <param name="contextName">The name of the DbContext that the current connection string is for.</param>
    /// <param name="providerKey">The key of the database provider.</param>
    /// <returns>The connection string specific for the provided context and provider.</returns>
    string GetConnectionString(string? contextName, string? providerKey);
}