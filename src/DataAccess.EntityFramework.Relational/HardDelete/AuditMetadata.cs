namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

internal static class AuditMetadata
{
    internal const string Prefix = "Basilisque:HardDeleteAudit:";
    internal const string Enabled = Prefix + "Enabled";
    internal const string Table = Prefix + "Table";
    internal const string Schema = Prefix + "Schema";
    internal const string UserType = Prefix + "UserType";
    internal const string KeyType = Prefix + "KeyType";
    internal const string KeyMembers = Prefix + "KeyMembers";
    internal const string SourceMembers = Prefix + "SourceMembers";
    internal const string AuditName = Prefix + "AuditName";
    internal const string SourceEntity = Prefix + "SourceEntity";
    internal const string DynamicKeyPrefix = "EntityKey_";
}
