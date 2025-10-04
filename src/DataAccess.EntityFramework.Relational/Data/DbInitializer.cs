using Basilisque.DataAccess.EntityFramework.Base.Data;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Relational.Data;

/// <summary>
/// Classes that inherit from <see cref="DbInitializer{TDbContext}"/> are being used to execute database initialization and seeding.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public abstract class DbInitializer<TDbContext> : BaseDbInitializer<TDbContext>
    where TDbContext : DbContext, IInitializableDbContext
{
    /// <summary>
    /// Creates a new <see cref="DbInitializer{TDbContext}"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext that should be initialized.</param>
    protected DbInitializer(TDbContext dbContext)
        : base(dbContext)
    { }

    /// <inheritdoc />
    protected override void ExecuteMigration()
    {
        DbContext.Database.Migrate();
    }
}
