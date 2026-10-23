using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Relational.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Basilisque.Demo.Data;

public sealed class DemoDbContext : BaseDbContext<DemoDbContext>
{
    private readonly ILoggerFactory? _loggerFactory;

    public DemoDbContext(IDbProviderServiceProvider services, ILoggerFactory? loggerFactory = null)
        : base(services)
    {
        _loggerFactory = loggerFactory;
    }

    public DbSet<Document> Documents => Set<Document>();

    protected override string GetAppAreaPrefix() => "Demo";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (_loggerFactory is not null)
            optionsBuilder.UseLoggerFactory(_loggerFactory);
    }

    protected override void OnAfterModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(document => document.Title).HasMaxLength(200);
            entity.Property(document => document.Content).HasMaxLength(4000);
        });
    }
}
