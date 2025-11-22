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

namespace Basilisque.DataAccess.EntityFramework.Base.Data;

/// <summary>
/// This interface is being used to find and execute all initializers; it should be implemented by all DbInitializers of the application.
/// </summary>
[RegisterServiceTransient(As = typeof(IDbInitializer))]
public interface IDbInitializer
{
    /// <summary>
    /// 1. Execute functionality before the database migration/creation.
    /// </summary>
    void BeforeMigration();

    /// <summary>
    /// 2. Execute the migration/creation of the database. Typically this method should only be used once in the whole application.
    /// </summary>
    void MigrateDatabase();

    /// <summary>
    /// 3. Execute functionality after the database migration/creation.
    /// </summary>
    void AfterMigration();

    /// <summary>
    /// 4. Used to seed the database with data.
    /// </summary>
    void SeedData();
}