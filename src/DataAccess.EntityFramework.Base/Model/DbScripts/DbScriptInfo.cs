namespace Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts;

/// <summary>
/// Represents a database script that will be execute during a migration; typically for creating or deleting database objects like stored procedures.
/// </summary>
public class DbScriptInfo
{
    /// <summary>
    /// The name of the script.
    /// </summary>
    public required string ScriptName { get; set; }

    /// <summary>
    /// The order in which the items should be processed.
    /// </summary>
    public required int Order { get; set; }

    /// <summary>
    /// The script that creates the database object.
    /// </summary>
    public required string CreateScript { get; set; }

    /// <summary>
    /// The script that deletes the database object.
    /// </summary>
    public string? DeleteScript { get; set; }
}
