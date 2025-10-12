using Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System.Diagnostics.CodeAnalysis;

namespace Basilisque.DataAccess.EntityFramework.Relational.Model.DbScripts;

//  EF1001: This is an internal API that supports the Entity Framework Core infrastructure and not subject to the same compatibility standards as public APIs. It may be
//          changed or removed without notice in any release. You should only use it directly in your code with extreme caution and knowing that doing so can result in application
//          failures when updating to a new Entity Framework Core release.
#pragma warning disable EF1001

/// <summary>
/// A custom model differ that can handle DB scripts by looking at the 'BAS:DA:DbScript:*' annotations created by the <see cref="ModelDbScriptsConvention{TDbContext}"/>.
/// </summary>
public class DbScriptsMigrationsModelDiffer : MigrationsModelDiffer
{
    /// <summary>
    /// Represents the type of difference in a comparison operation.
    /// </summary>
    /// <remarks>This enumeration is used to indicate whether an item was added, modified or removed during a comparison process.</remarks>
    private enum DiffType
    {
        Added,
        Modified,
        Removed
    }

    /// <summary>
    /// Represents a difference between two DB scripts.
    /// </summary>
    private sealed record DbScriptDiff(DiffType DiffType, DbScriptInfo? Source, DbScriptInfo? Target);

    /// <summary>
    /// Creates a new <see cref="DbScriptsMigrationsModelDiffer"/>.
    /// </summary>
    public DbScriptsMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies
        )
        : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
    { }

    /// <inheritdoc />
    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
    {
        // retrieve 'BAS:DA:DbScript:...' annotations from source and target models
        var sourceDbScripts = getDbScriptAnnotations(source);
        var targetDbScripts = getDbScriptAnnotations(target);

        // compare db scripts and identify differences
        var scriptDifferences = compareDbScripts(sourceDbScripts, targetDbScripts).ToList();

        // get base differences from the base class
        var baseDifferences = base.GetDifferences(source, target);

        // if there are no script differences, return base differences
        if (!scriptDifferences.Any())
            return baseDifferences;

        var result = new List<MigrationOperation>(baseDifferences);

        foreach (var difference in scriptDifferences)
        {
            if (shouldExecuteDeleteScript(difference, out string? deleteScript))
            {
                // add delete script operation at the beginning
                // (in reverse order to ensure dependencies are handled correctly)
                result.Insert(0, new SqlOperation
                {
                    Sql = deleteScript,
                    IsDestructiveChange = true
                });
            }

            if (shouldExecuteCreateScript(difference, out string? createScript))
            {
                // add create script operation at the end
                result.Add(new SqlOperation
                {
                    Sql = createScript,
                    IsDestructiveChange = false
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Compares source and target db scripts and returns a list of differences.
    /// </summary>
    /// <param name="sourceDbScripts">Scripts from the source model.</param>
    /// <param name="targetDbScripts">Scripts from the target model.</param>
    /// <returns>A list of script differences.</returns>
    private IEnumerable<DbScriptDiff> compareDbScripts(List<DbScriptInfo> sourceDbScripts, List<DbScriptInfo> targetDbScripts)
    {
        // create dictionaries from source scripts for faster lookup
        var sourceDict = sourceDbScripts.ToDictionary(s => s.ScriptName, s => s);
        var targetDict = targetDbScripts.ToDictionary(s => s.ScriptName, s => s);

        // first iterate throug target scripts to preserve their order
        foreach (var targetScript in targetDbScripts)
        {
            if (sourceDict.TryGetValue(targetScript.ScriptName, out var sourceScript))
            {
                // script exists in both, compare CreateScript and DeleteScript
                if (!string.Equals(sourceScript.CreateScript, targetScript.CreateScript, StringComparison.Ordinal)
                    || !string.Equals(sourceScript.DeleteScript, targetScript.DeleteScript, StringComparison.Ordinal))
                {
                    // content changed
                    yield return new(DiffType.Modified, sourceScript, targetScript);
                }
            }
            else
            {
                // script exists only in target (was added)
                yield return new(DiffType.Added, null, targetScript);
            }
        }

        // then check for scripts that exist only in source (were removed)
        foreach (var sourceScript in sourceDbScripts)
        {
            if (targetDict.ContainsKey(sourceScript.ScriptName))
                continue;

            yield return new(DiffType.Removed, sourceScript, null);
        }
    }

    private List<DbScriptInfo> getDbScriptAnnotations(IRelationalModel? relationalModel)
    {
        if (relationalModel is null)
            return new List<DbScriptInfo>();

        return relationalModel.Model.GetAnnotations()
            .Where(a => a.Name.StartsWith("BAS:DA:DbScript:", StringComparison.Ordinal) && a.Value is not null)
            .Select(a =>
            {
                var info = System.Text.Json.JsonSerializer.Deserialize<DbScriptInfo>(a.Value!.ToString()!)!;
                return info;
            })
            .OrderBy(i => i.Order)
            .ToList();
    }

    private bool shouldExecuteDeleteScript(DbScriptDiff difference, [NotNullWhen(true)] out string? deleteScript)
    {
        deleteScript = difference.Source?.DeleteScript;

        if (difference.DiffType == DiffType.Added)
            return false;

        if (string.IsNullOrWhiteSpace(deleteScript))
            return false;

        return true;
    }

    private bool shouldExecuteCreateScript(DbScriptDiff difference, [NotNullWhen(true)] out string? createScript)
    {
        createScript = difference.Source?.CreateScript;

        if (difference.DiffType == DiffType.Removed)
            return false;

        if (string.IsNullOrWhiteSpace(createScript))
            throw new InvalidOperationException();

        return true;
    }
}

#pragma warning restore EF1001 // Internal EF Core API usage.