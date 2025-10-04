using Basilisque.DataAccess.EntityFramework.Base.Provider;

namespace Basilisque.DataAccess.EntityFramework.SqlServer.Connection;

/// <inheritdoc />
internal class SqlServerDbProviderInfo : BaseDbProviderInfo
{
    internal const string ProviderKeyName = "SqlServer";

    /// <inheritdoc />
    public override string ProviderKey => ProviderKeyName;

    /// <inheritdoc />
    public override string ProviderName => "Microsoft.EntityFrameworkCore.SqlServer";
}
