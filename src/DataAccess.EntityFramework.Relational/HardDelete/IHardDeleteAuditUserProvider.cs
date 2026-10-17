using Basilisque.Core.Auth;

namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>Resolves actors for a configured non-nullable user-key type.</summary>
public interface IHardDeleteAuditUserProvider
{
    /// <summary>Gets the non-nullable user-key type.</summary>
    Type UserKeyType { get; }

    /// <summary>Gets the actor, or null when there is no current actor.</summary>
    object? GetCurrentUserId();
}

/// <summary>Uses the same Core user context as entity stamping.</summary>
/// <typeparam name="TUser">The non-nullable user-key type.</typeparam>
public sealed class HardDeleteAuditUserProvider<TUser>(IUserContext<TUser> userContext) : IHardDeleteAuditUserProvider
    where TUser : notnull
{
    /// <inheritdoc />
    public Type UserKeyType => typeof(TUser);

    /// <inheritdoc />
    public object? GetCurrentUserId() => userContext.TryGetCurrentUserId(out var userId) ? userId : null;
}
