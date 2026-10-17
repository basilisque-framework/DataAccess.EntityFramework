using Basilisque.DataAccess.EntityFramework.Relational.Design;
using Basilisque.DataAccess.EntityFramework.Relational.HardDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Relational.Unit.Tests.Design;

public class RelationalBaseDesignTimeDbContextFactoryTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Registers_one_scoped_interceptor_for_standalone_and_child_factories(bool isChildFactory)
    {
        var services = new ServiceCollection();
        var factory = new RelationalBaseDesignTimeDbContextFactory<DbContext>();
        factory.ConfigureProviderServices(services, [], isChildFactory);
        factory.ConfigureProviderServices(services, [], isChildFactory);
        await Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(HardDeleteAuditInterceptor))).IsEqualTo(1);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var interceptor = first.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>();
        await Assert.That(ReferenceEquals(interceptor, first.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>())).IsTrue();
        await Assert.That(ReferenceEquals(interceptor, second.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>())).IsFalse();
    }

    [Test]
    public async Task Preserves_explicit_interceptor_registration()
    {
        var services = new ServiceCollection();
        var interceptor = new HardDeleteAuditInterceptor([]);
        services.AddScoped(_ => interceptor);
        new RelationalBaseDesignTimeDbContextFactory<DbContext>().ConfigureProviderServices(services, [], true);
        await Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(HardDeleteAuditInterceptor))).IsEqualTo(1);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await Assert.That(ReferenceEquals(scope.ServiceProvider.GetRequiredService<HardDeleteAuditInterceptor>(), interceptor)).IsTrue();
    }
}
