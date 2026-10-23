using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Demo.Data;

public enum DocumentAction
{
    Update,
    SoftDelete,
    Restore,
    HardDelete
}

public enum DocumentView
{
    Active,
    Deleted,
    All
}

public sealed record DocumentSnapshot(
    Guid Id, string Title, string Content,
    DateTimeOffset CreatedAt, Guid CreatedBy,
    DateTimeOffset ModifiedAt, Guid ModifiedBy,
    DateTimeOffset? DeletedAt, Guid? DeletedBy)
{
    public static DocumentSnapshot From(Document document) => new(
        document.Id, document.Title, document.Content,
        document.CreatedAt, document.CreatedBy, document.ModifiedAt, document.ModifiedBy,
        document.DeletedAt, document.DeletedBy);
}

public sealed record ActionResult(string Operation, DocumentSnapshot? Before, DocumentSnapshot? After);

public sealed record DemoSnapshot(
    IReadOnlyList<DocumentSnapshot> Documents,
    IReadOnlyList<HardDeleteAudit<Guid>> Deletions);

[RegisterServiceScoped]
public sealed class DocumentScenarios
{
    private readonly DemoDbContext _context;

    public DocumentScenarios(DemoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ResetAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Documents.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _context.HardDeleteAudits<Document, Guid>().ExecuteDeleteAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<DemoSnapshot> ReadAsync(DocumentView view)
    {
        IQueryable<Document> query = _context.Documents.AsNoTracking();
        if (view != DocumentView.Active)
            query = query.IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName]);
        if (view == DocumentView.Deleted)
            query = query.Where(document => document.DeletedAt != null);

        var documents = await query.OrderBy(document => document.Title).ToListAsync();
        var audits = await _context.HardDeleteAudits<Document, Guid>().AsNoTracking().ToListAsync();
        // SQLite cannot order DateTimeOffset values in SQL.
        return new DemoSnapshot(documents.Select(DocumentSnapshot.From).ToList(),
            audits.OrderByDescending(audit => audit.DeletedAt).ToList());
    }

    public async Task<ActionResult> CreateAsync(string title, string content)
    {
        validateText(title, content);
        var document = new Document { Title = title.Trim(), Content = content };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return new ActionResult("Create: Add + SaveChangesAsync", null, DocumentSnapshot.From(document));
    }

    public async Task<ActionResult> ExecuteAsync(Guid id, DocumentAction action, string title, string content)
    {
        var document = await _context.Documents
            .IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName])
            .SingleAsync(document => document.Id == id);
        var before = DocumentSnapshot.From(document);

        switch (action)
        {
            case DocumentAction.Update:
                if (document.DeletedAt is not null)
                    throw new InvalidOperationException("Restore the document before editing it.");
                validateText(title, content);
                document.Title = title.Trim();
                document.Content = content;
                break;
            case DocumentAction.SoftDelete:
                _context.Remove(document);
                break;
            case DocumentAction.Restore:
                if (document.DeletedAt is null)
                    throw new InvalidOperationException("Only deleted documents can be restored.");
                document.DeletedAt = null;
                break;
            case DocumentAction.HardDelete:
                using (SoftDeleteSuppressor.AllowHardDelete())
                {
                    _context.Remove(document);
                    await _context.SaveChangesAsync();
                }
                return new ActionResult("Hard delete: AllowHardDelete + Remove + SaveChangesAsync", before, null);
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        await _context.SaveChangesAsync();
        return new ActionResult($"{action}: SaveChangesAsync", before, DocumentSnapshot.From(document));
    }

    private static void validateText(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new ArgumentException("A title between 1 and 200 characters is required.", nameof(title));
        if (content.Length > 4000)
            throw new ArgumentException("Content must not exceed 4000 characters.", nameof(content));
    }
}
