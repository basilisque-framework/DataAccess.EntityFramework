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