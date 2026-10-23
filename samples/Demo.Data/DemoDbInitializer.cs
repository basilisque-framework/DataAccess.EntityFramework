using Basilisque.Core.Auth;
using Basilisque.DataAccess.EntityFramework.Relational.Data;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Demo.Data;

public sealed class DemoDbInitializer : DbInitializer<DemoDbContext>
{
    private readonly IWritableUserContext<Guid> _userContext;

    public DemoDbInitializer(DemoDbContext context, IWritableUserContext<Guid> userContext)
        : base(context)
    {
        _userContext = userContext;
    }

    public override void SeedData()
    {
        if (DbContext.Documents.IgnoreQueryFilters().Any() || DbContext.HardDeleteAudits<Document, Guid>().Any())
            return;

        _userContext.UserId = DemoUsers.Alice;
        DbContext.Documents.Add(new Document
        {
            Title = "Welcome to Basilisque",
            Content = "Edit, delete and restore this document to explore the library."
        });
        DbContext.SaveChanges();
    }
}
