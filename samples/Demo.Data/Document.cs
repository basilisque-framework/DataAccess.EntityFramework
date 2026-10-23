using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

namespace Basilisque.Demo.Data;

public sealed class Document : IStampChanges, ISoftDelete, IHardDeleteAudited
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
