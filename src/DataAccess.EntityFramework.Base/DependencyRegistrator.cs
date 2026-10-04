using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.DataAccess.EntityFramework.Base;

public partial class DependencyRegistrator
{
    partial void doAfterRegistration(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStampHandler, TimestampStampHandler>());
        services.TryAddScoped(typeof(IUserStampHandler<>), typeof(UserStampHandler<>));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStampHandler, UserStampDispatcher>());
    }
}
