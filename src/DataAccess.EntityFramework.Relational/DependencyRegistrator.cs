using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.DataAccess.EntityFramework.Relational;

public partial class DependencyRegistrator
{
    partial void doAfterRegistration(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHardDeleteAuditUserProvider, HardDeleteAuditUserProvider<Guid>>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHardDeleteAuditUserProvider, HardDeleteAuditUserProvider<string>>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHardDeleteAuditUserProvider, HardDeleteAuditUserProvider<int>>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHardDeleteAuditUserProvider, HardDeleteAuditUserProvider<long>>());
    }
}
