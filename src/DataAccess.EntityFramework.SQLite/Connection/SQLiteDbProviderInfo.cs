using Basilisque.DataAccess.EntityFramework.Base.Provider;

namespace Basilisque.DataAccess.EntityFramework.SQLite.Connection;

/// <inheritdoc />
internal class SQLiteDbProviderInfo : BaseDbProviderInfo
{
    internal const string ProviderKeyName = "SQLite";

    /// <inheritdoc />
    public override string ProviderKey => ProviderKeyName;

    /// <inheritdoc />
    public override string ProviderName => "Microsoft.EntityFrameworkCore.Sqlite";
}