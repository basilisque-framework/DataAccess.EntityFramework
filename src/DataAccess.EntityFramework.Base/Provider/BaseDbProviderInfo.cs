namespace Basilisque.DataAccess.EntityFramework.Base.Provider;

/// <inheritdoc />
public abstract class BaseDbProviderInfo : IDbProviderInfo
{
    /// <inheritdoc />
    public abstract string ProviderKey { get; }

    /// <inheritdoc />
    public abstract string ProviderName { get; }
}
