namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>
/// A physical deletion event with a typed entity key and actor.
/// </summary>
/// <typeparam name="TKey">The entity key or configured composite key object.</typeparam>
/// <typeparam name="TUser">The nullable actor type, for example Guid? or string.</typeparam>
public class HardDeleteAudit<TKey, TUser>
{
    /// <summary>Gets or sets the unique event identifier.</summary>
    public Guid AuditId { get; set; }

    /// <summary>Gets or sets the deleted entity's key.</summary>
    public TKey EntityKey { get; set; } = default!;

    /// <summary>Gets or sets the application's UTC deletion timestamp.</summary>
    public DateTimeOffset DeletedAt { get; set; }

    /// <summary>Gets or sets the actor, or null when no actor was available.</summary>
    public TUser DeletedBy { get; set; } = default!;
}

/// <summary>
/// A physical deletion event with a nullable Guid actor.
/// </summary>
/// <typeparam name="TKey">The entity key or configured composite key object.</typeparam>
public sealed class HardDeleteAudit<TKey> : HardDeleteAudit<TKey, Guid?>
{
}
