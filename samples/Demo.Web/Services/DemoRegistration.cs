namespace Basilisque.Demo.Web.Services;

/// <summary>Registers the sample and its provider dependencies.</summary>
public static class DemoRegistration
{
    /// <summary>Registers generated application and framework services.</summary>
    /// <param name="services">The application services.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The application services.</returns>
    public static IServiceCollection AddDemo(this IServiceCollection services, IConfiguration configuration)
    {
        Basilisque.Demo.Web.IServiceCollectionExtensions.RegisterServices(services);
        return services;
    }
}
