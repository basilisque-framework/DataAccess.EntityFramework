using Basilisque.DataAccess.EntityFramework.Base.Provider;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Connection;

/// <inheritdoc />
internal class PostgreSQLDbProviderInfo : BaseDbProviderInfo
{
    internal const string ProviderKeyName = "PostgreSQL";

    /// <inheritdoc />
    public override string ProviderKey => ProviderKeyName;

    /// <inheritdoc />
    public override string ProviderName => "Npgsql.EntityFrameworkCore.PostgreSQL";
}