using Basilisque.DataAccess.EntityFramework.PostgreSQL.Design;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.PostgreSQL.Unit.Tests.Design;

public class HardDeleteAuditRegistrationTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Provider_factory_registers_one_scoped_audit_interceptor(bool isChildFactory)
    {
        var services = new ServiceCollection();
        var factory = new PostgreSQLBaseDesignTimeDbContextFactory<DbContext>();
        factory.ConfigureProviderServices(services, [], isChildFactory);
        factory.ConfigureProviderServices(services, [], isChildFactory);
        await Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(HardDeleteAuditInterceptor))).IsEqualTo(1);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var interceptor = first.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>();
        await Assert.That(ReferenceEquals(interceptor, first.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>())).IsTrue();
        await Assert.That(ReferenceEquals(interceptor, second.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>())).IsFalse();
    }
}
