using Basilisque.Core.Auth;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.DataAccess.EntityFramework.Base;

public partial class DependencyRegistrator
{
    partial void doAfterRegistration(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStampHandler, TimestampStampHandler>());
        addDefaultUserHandler<Guid>(services);
        addDefaultUserHandler<string>(services);
        addDefaultUserHandler<int>(services);
        addDefaultUserHandler<long>(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStampHandler, UserStampDispatcher>());
    }

    private static void addDefaultUserHandler<TKey>(IServiceCollection services)
        where TKey : notnull
    {
        tryAddClosedCoreService(services, typeof(WritableUserContext<>), typeof(WritableUserContext<>),
            static _ => new WritableUserContext<TKey>());
        tryAddClosedCoreService<IUserContext<TKey>>(services, typeof(IUserContext<>), typeof(UserContextProxy<>),
            static sp => new UserContextProxy<TKey>(sp.GetRequiredService<WritableUserContext<TKey>>()));
        tryAddClosedCoreService<IWritableUserContext<TKey>>(services, typeof(IWritableUserContext<>), typeof(WritableUserContextProxy<>),
            static sp => new WritableUserContextProxy<TKey>(sp.GetRequiredService<WritableUserContext<TKey>>()));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserStampHandler, UserStampHandler<TKey>>(
            static sp => new UserStampHandler<TKey>(sp.GetRequiredService<IUserContext<TKey>>())));
    }

    private static void tryAddClosedCoreService<TService>(IServiceCollection services, Type openServiceType,
        Type defaultImplementationType, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        var registration = services.LastOrDefault(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == openServiceType);

        // Close only Core's defaults so custom open-generic implementations are not shadowed.
        if (registration?.ImplementationType == defaultImplementationType && registration.Lifetime == ServiceLifetime.Scoped)
            services.TryAddScoped(factory);
    }
}
