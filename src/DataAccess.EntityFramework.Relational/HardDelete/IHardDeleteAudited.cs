namespace Basilisque.DataAccess.EntityFramework.Relational.HardDelete;

/// <summary>
/// Enables hard-delete auditing with a nullable Guid actor and inferred primary key.
/// </summary>
public interface IHardDeleteAudited : IHardDeleteAudited<Guid>
{
}

/// <summary>
/// Enables hard-delete auditing with the specified actor key type.
/// </summary>
/// <typeparam name="TUser">The non-nullable actor key type.</typeparam>
public interface IHardDeleteAudited<TUser> where TUser : notnull
{
}
