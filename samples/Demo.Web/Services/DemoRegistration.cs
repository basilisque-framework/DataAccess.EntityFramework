using Basilisque.DataAccess.EntityFramework.Base.Model;

namespace Basilisque.Demo.Web.Services;

public static class DemoRegistration
{
    public static IServiceCollection AddDemo(this IServiceCollection services, IConfiguration configuration)
    {
        Basilisque.Demo.Web.IServiceCollectionExtensions.RegisterServices(services);

        // All three migration assemblies are dependencies. Select one explicitly after the generated chain.
        IMigrationAssemblyProvider migrations = configuration["Database:Provider"] switch
        {
            "SQLite" => new SQLite.Generated.MigrationAssemblyProvider(),
            "SqlServer" => new SqlServer.Generated.MigrationAssemblyProvider(),
            "PostgreSQL" => new PostgreSQL.Generated.MigrationAssemblyProvider(),
            _ => throw new InvalidOperationException("Database:Provider must be SQLite, SqlServer or PostgreSQL.")
        };
        services.AddSingleton(migrations);
        return services;
    }
}
