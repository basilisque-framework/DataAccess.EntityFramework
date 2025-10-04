using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Data;

/// <summary>
/// Classes that inherit from <see cref="BaseDbInitializer{TDbContext}"/> are being used to execute database initialization and seeding.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public abstract class BaseDbInitializer<TDbContext> : IDbInitializer
    where TDbContext : DbContext, IInitializableDbContext
{
    /// <summary>
    /// The DbContext that should be initialized.
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <summary>
    /// Creates a new <see cref="BaseDbInitializer{TDbContext}"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext that should be initialized.</param>
    protected BaseDbInitializer(TDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        DbContext = dbContext;
    }

    /// <inheritdoc />
    public virtual void BeforeMigration()
    { /* for overriding purposes only */ }

    /// <inheritdoc />
    public virtual void AfterMigration()
    { /* for overriding purposes only */ }

    /// <inheritdoc />
    public void MigrateDatabase()
    {
        if (DbContext.WasAlreadyMigrated)
            return;

        ExecuteMigration();

        DbContext.WasAlreadyMigrated = true;
    }

    /// <inheritdoc />
    public virtual void SeedData()
    { /* for overriding purposes only */ }

    /// <summary>
    /// Executes the database migration.
    /// </summary>
    protected abstract void ExecuteMigration();
}
