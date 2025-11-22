/*
   Copyright 2025 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Basilisque.DataAccess.EntityFramework.Base.Model.DbScripts;

/// <summary>
/// A convention to import db scripts into the model.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context to import the scripts for.</typeparam>
public class ModelDbScriptsConvention<TDbContext> : IModelFinalizingConvention
    where TDbContext : BaseDbContext<TDbContext>
{
    private readonly BaseDbContext<TDbContext> _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelDbScriptsConvention{TDbContext}"/> class using the specified database context.
    /// </summary>
    /// <param name="dbContext">The database context to be used by the convention. Cannot be null.</param>
    public ModelDbScriptsConvention(
        BaseDbContext<TDbContext> dbContext
        )
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    /// <summary>
    /// Searches for a DB scripts folder relative to the <typeparamref name="TDbContext"/> source file and imports scripts from there according to the <see cref="BaseDbContext{TDbContext}.GetDbScripts()"/> method into the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance representing the current state of the model being constructed. Cannot be null.</param>
    /// <param name="context">The context object that provides information and control for the convention execution. Cannot be null.</param>
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var dbContextSourceFilePath = _dbContext.DbContextSourceFilePath;
        if (string.IsNullOrWhiteSpace(dbContextSourceFilePath))
            return;

        var providerDirectoryName = _dbContext.GetProviderDirectoryName();
        if (string.IsNullOrWhiteSpace(providerDirectoryName))
            return;

        var baseDirectory = System.IO.Directory.GetParent(dbContextSourceFilePath);
        if (baseDirectory is null)
            return;

        var scriptsDirectory = System.IO.Path.Combine(baseDirectory.FullName, "DbScripts", providerDirectoryName);
        if (!System.IO.Directory.Exists(scriptsDirectory))
            return;

        addDbScriptsToModel(modelBuilder, scriptsDirectory);
    }

    private void addDbScriptsToModel(IConventionModelBuilder modelBuilder, string scriptsDirectory)
    {
        int i = -1;
        foreach (var dbScriptName in _dbContext.GetDbScripts())
        {
            i++;

            var createScript = readCreateScriptFileContent(scriptsDirectory, dbScriptName, modelBuilder);
            var deleteScript = readDeleteScriptFileContent(scriptsDirectory, dbScriptName, modelBuilder);

            var info = new DbScriptInfo()
            {
                Order = i,
                ScriptName = dbScriptName,
                CreateScript = createScript,
                DeleteScript = deleteScript
            };

            var annotation = System.Text.Json.JsonSerializer.Serialize(info);

            modelBuilder.HasAnnotation($"BAS:DA:DbScript:{dbScriptName}", annotation);
        }
    }

    private string readCreateScriptFileContent(string scriptsDirectory, string dbScriptName, IConventionModelBuilder modelBuilder)
    {
        var result = readScriptFileContent(scriptsDirectory, dbScriptName, modelBuilder, ".Create");
        if (result is not null)
            return result;

        result = readScriptFileContent(scriptsDirectory, dbScriptName, modelBuilder, "");

        if (result is null)
            throw new InvalidOperationException($"Could not find create script for '{dbScriptName}' in '{scriptsDirectory}'.");

        return result;
    }

    private string? readDeleteScriptFileContent(string scriptsDirectory, string dbScriptName, IConventionModelBuilder modelBuilder)
    {
        return readScriptFileContent(scriptsDirectory, dbScriptName, modelBuilder, ".Delete");
    }

    private string? readScriptFileContent(string scriptsDirectory, string dbScriptName, IConventionModelBuilder modelBuilder, string suffix)
    {
        var scriptFileName = $"{dbScriptName}{suffix}.sql";

        var filePath = System.IO.Path.Combine(scriptsDirectory, scriptFileName);
        if (!System.IO.File.Exists(filePath))
            return null;

        var result = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        return ReplacePlaceholders(result, dbScriptName, modelBuilder);
    }

    /// <summary>
    /// Replaces placeholders in the db script content.
    /// </summary>
    /// <param name="dbScriptContent">The db script content.</param>
    /// <param name="dbScriptName">The name of the db script.</param>
    /// <param name="modelBuilder">The model builder instance representing the current state of the model being constructed. Cannot be null.</param>
    /// <returns>The db script content with replaced placeholders.</returns>
    protected virtual string ReplacePlaceholders(string dbScriptContent, string dbScriptName, IConventionModelBuilder modelBuilder)
    {
        var result = replaceScriptName(dbScriptContent, dbScriptName);

        return result;
    }

    private string replaceScriptName(string dbScriptContent, string dbScriptName)
    {
        return dbScriptContent.Replace("{{ScriptName}}", dbScriptName, StringComparison.OrdinalIgnoreCase);
    }
}
