using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basilisque.DataAccess.EntityFramework.Relational.Model.DbScripts;

/// <inheritdoc />
public class ModelDbScriptsConvention<TDbContext> : Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts.ModelDbScriptsConvention<TDbContext>
    where TDbContext : BaseDbContext<TDbContext>
{
    /// <summary>
    /// Creates a new <see cref="ModelDbScriptsConvention{TDbContext}"/>
    /// </summary>
    /// <param name="dbContext">The database context to be used by the convention. Cannot be null.</param>
    public ModelDbScriptsConvention(
        Base.Model.BaseDbContext<TDbContext> dbContext
        )
        : base(dbContext)
    { }

    /// <inheritdoc />
    protected override string ReplacePlaceholders(string dbScriptContent, string dbScriptName, IConventionModelBuilder modelBuilder)
    {
        var result = base.ReplacePlaceholders(dbScriptContent, dbScriptName, modelBuilder);

        result = replaceSchema(result, modelBuilder);

        return result;
    }

    private string replaceSchema(string dbScriptContent, IConventionModelBuilder modelBuilder)
    {
        var defaultSchema = modelBuilder.Metadata.GetDefaultSchema() ?? "dbo";

        return dbScriptContent.Replace("{{Schema}}", defaultSchema, StringComparison.OrdinalIgnoreCase);
    }
}
