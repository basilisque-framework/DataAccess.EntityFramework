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

using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Relational.Model;

/// <summary>
/// A base class with common functionality for relational DbContexts
/// </summary>
public abstract class BaseDbContext<TDbContext> : Basilisque.DataAccess.EntityFramework.Base.Model.BaseDbContext<TDbContext>
    where TDbContext : BaseDbContext<TDbContext>
{
    /// <summary>
    /// Creates a new <see cref="BaseDbContext{TDbContext}"/>.
    /// </summary>
    /// <param name="dbProviderServiceProvider">The <see cref="IDbProviderServiceProvider"/> that is used to resolve database provider specific services.</param>
    protected BaseDbContext(
        IDbProviderServiceProvider dbProviderServiceProvider
        )
        : base(dbProviderServiceProvider)
    { }

    /// <summary>
    /// Gets the app area prefix for the table names.
    /// </summary>
    /// <returns>The app area prefix for the table name.</returns>
    protected abstract string GetAppAreaPrefix();

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="DbSet{TEntity}" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context (Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel))
    ///         then this method will not be run. However, it will still run when creating a compiled model.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more information and
    ///         examples.
    ///     </para>
    /// </remarks>
    /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically define extension methods on this object that allow you to configure aspects of the model that are specific to a given database.</param>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnBeforeModelCreating(modelBuilder);

        base.OnModelCreating(modelBuilder);

        prefixTableNames(modelBuilder);

        OnAfterModelCreating(modelBuilder);
    }

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="DbSet{TEntity}" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context (Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel))
    ///         then this method will not be run. However, it will still run when creating a compiled model.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more information and
    ///         examples.
    ///     </para>
    /// </remarks>
    /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically define extension methods on this object that allow you to configure aspects of the model that are specific to a given database.</param>
    protected virtual void OnBeforeModelCreating(ModelBuilder modelBuilder)
    { /* for overriding purposes only */ }

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="DbSet{TEntity}" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If a model is explicitly set on the options for this context (Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel))
    ///         then this method will not be run. However, it will still run when creating a compiled model.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more information and
    ///         examples.
    ///     </para>
    /// </remarks>
    /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically define extension methods on this object that allow you to configure aspects of the model that are specific to a given database.</param>
    protected virtual void OnAfterModelCreating(ModelBuilder modelBuilder)
    { /* for overriding purposes only */ }

    /// <inheritdoc />
    protected override void AddDesignTimeModelDbScriptsConvention(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(sp => new DbScripts.ModelDbScriptsConvention<TDbContext>(this));
    }

    private void prefixTableNames(ModelBuilder modelBuilder)
    {
        var appArea = GetAppAreaPrefix();

        if (string.IsNullOrWhiteSpace(appArea))
            throw new ArgumentNullException(appArea);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName($"{appArea}_{entity.GetTableName()}");
        }
    }
}
