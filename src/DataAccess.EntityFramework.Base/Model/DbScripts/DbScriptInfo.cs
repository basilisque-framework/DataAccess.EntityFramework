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

namespace Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts;

/// <summary>
/// Represents a database script that will be execute during a migration; typically for creating or deleting database objects like stored procedures.
/// </summary>
public class DbScriptInfo
{
    /// <summary>
    /// The name of the script.
    /// </summary>
    public required string ScriptName { get; set; }

    /// <summary>
    /// The order in which the items should be processed.
    /// </summary>
    public required int Order { get; set; }

    /// <summary>
    /// The script that creates the database object.
    /// </summary>
    public required string CreateScript { get; set; }

    /// <summary>
    /// The script that deletes the database object.
    /// </summary>
    public string? DeleteScript { get; set; }
}
