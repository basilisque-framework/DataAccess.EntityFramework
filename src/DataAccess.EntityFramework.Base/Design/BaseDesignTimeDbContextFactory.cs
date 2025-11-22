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

using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Design;

/// <inheritdoc cref="IDesignTimeDbContextFactory{TDbContext}" />
public abstract class BaseDesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private static IServiceScope? _scope;

    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var serviceProvider = createServiceProvider();

        _scope ??= serviceProvider.CreateScope();

        return CreateDbContext(_scope.ServiceProvider, args);
    }

    /// <summary>
    /// Override this method in the provider specific implementation to configure the <see cref="IServiceCollection"/> that is used to create the <see cref="DbContext"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> that is being initialized.</param>
    protected virtual void ConfigureProviderServices(IServiceCollection services)
    { /* for overriding purposes only */ }

    /// <summary>
    /// Override this method to configure the <see cref="IServiceCollection"/> that is used to create the <see cref="DbContext"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> that is being initialized.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    { /* for overriding purposes only */ }

    /// <summary>
    /// Creates the <see cref="IConfiguration"/> that is used during design time.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> that can be used to get services.</param>
    /// <returns>The <see cref="IConfiguration"/> that is used during design time.</returns>
    protected virtual IConfiguration CreateConfiguration(IServiceProvider serviceProvider)
    {
        var currentDirectory = GetAppSettingsPath(System.IO.Directory.GetCurrentDirectory());
        return new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Searches for the appsettings.json file and returns the directory path where it is located.
    /// </summary>
    /// <param name="projectDirectory">The directory to start the search in.</param>
    /// <returns>The path where the appsettings.json file is located.</returns>
    protected virtual string GetAppSettingsPath(string projectDirectory)
    {
        var currentDirectory = projectDirectory;

        while (currentDirectory is not null)
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(currentDirectory, "appsettings.json")))
                return currentDirectory;

            if (System.IO.File.Exists(System.IO.Path.Combine(currentDirectory, "Service\\appsettings.json")))
                return System.IO.Path.Combine(currentDirectory, "Service");

            currentDirectory = System.IO.Path.GetDirectoryName(currentDirectory);
        }

        return projectDirectory;
    }

    /// <summary>
    /// Creates the <see cref="DbContext"/>. Override this method to provide a custom implementation.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> that is used to create the <see cref="DbContext"/>.</param>
    /// <param name="args">The arguments provided by the design-time service.</param>
    /// <returns>The new instance of the <see cref="DbContext"/>.</returns>
    protected virtual TDbContext CreateDbContext(IServiceProvider serviceProvider, string[] args)
    {
        var result = serviceProvider.GetRequiredService<TDbContext>();

        if (result is IDbContextDesignSupport dbContextDesignSupport)
            dbContextDesignSupport.IsDesignTime = true;

        return result;
    }

    private ServiceProvider createServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();

        configureServices(services);

        ConfigureProviderServices(services);

        ConfigureServices(services);

        return services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void configureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(sp => CreateConfiguration(sp));
        services.AddSingleton<IDbProviderServiceProvider, DbProviderServiceProvider>();
        services.AddSingleton<IConnectionStringBuilder, ConnectionStringBuilder>();

        services.AddTransient<TDbContext>();
    }
}
