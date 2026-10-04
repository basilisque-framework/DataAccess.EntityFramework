/*
   Copyright 2026 Alexander Stärk

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

using Basilisque.Core.Auth;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Base.Provider;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Stamping;

public class StampRegistrationTests
{
    private sealed class StampedEntity : IStampChanges
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
    }

    private sealed class StampingDbContext : BaseDbContext<StampingDbContext>
    {
        public StampingDbContext(IDbProviderServiceProvider serviceProvider) : base(serviceProvider)
        { }

        public DbSet<StampedEntity> Entities => Set<StampedEntity>();
        public DbSet<StringStampedEntity> StringEntities => Set<StringStampedEntity>();
        public DbSet<ShadowStampedEntity> ShadowEntities => Set<ShadowStampedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ShadowStampedEntity>()
                .UseShadowCreationStamp<ShadowStampedEntity, int>()
                .UseShadowModificationStamp<ShadowStampedEntity, int>();
        }
    }

    private sealed class StringStampedEntity : IStampCreateUser<string>, IStampModifyUser<string>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }

    private sealed class ShadowStampedEntity
    {
        public Guid Id { get; set; }
    }

    private sealed class CustomStringHandler : IUserStampHandler<string>
    {
        public int InvocationCount { get; private set; }

        public void UpdateStampProperties(DbContext context, DateTimeOffset timestamp)
        {
            InvocationCount++;
        }
    }

    private sealed class InMemoryConfigurator : BaseDbContextOptionsConfigurator
    {
        private readonly string _databaseName = Guid.NewGuid().ToString("N");

        protected override void OnConfigure<TDbContext>(BaseDbContext<TDbContext> dbContext, DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(_databaseName);
        }
    }

    private sealed class DesignTimeFactory : BaseDesignTimeDbContextFactory<StampingDbContext>
    {
        protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args)
        {
            return createConfiguration();
        }

        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddSingleton<IDbContextOptionsConfigurator, InMemoryConfigurator>();
            var providerInfo = Substitute.For<IDbProviderInfo>();
            providerInfo.ProviderKey.Returns("InMemory");
            providerInfo.ProviderName.Returns("InMemory");
            services.AddSingleton(providerInfo);
        }
    }

    [Test]
    public async Task Default_registration_adds_one_timestamp_handler_and_one_user_dispatcher()
    {
        using var provider = createProvider();
        using var scope = provider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IStampHandler>().ToArray();

        await Assert.That(handlers.OfType<TimestampStampHandler>().Count()).IsEqualTo(1);
        await Assert.That(handlers.Length).IsEqualTo(2);
        await Assert.That(scope.ServiceProvider.GetRequiredService<IUserStampHandler<Guid>>() is UserStampHandler<Guid>).IsTrue();
        await Assert.That(scope.ServiceProvider.GetRequiredService<IUserStampHandler<string>>() is UserStampHandler<string>).IsTrue();
    }

    [Test]
    public async Task Default_registration_keeps_interceptors_scoped_and_provider_wrappers_transient()
    {
        using var provider = createProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstWrapper = firstScope.ServiceProvider.GetRequiredService<IDbProviderServiceProvider>();
        var anotherWrapper = firstScope.ServiceProvider.GetRequiredService<IDbProviderServiceProvider>();
        var secondWrapper = secondScope.ServiceProvider.GetRequiredService<IDbProviderServiceProvider>();
        var firstInterceptor = firstWrapper.GetRequiredService<CompositeStampInterceptor>();

        await Assert.That(ReferenceEquals(firstWrapper, anotherWrapper)).IsFalse();
        await Assert.That(ReferenceEquals(firstInterceptor, anotherWrapper.GetRequiredService<CompositeStampInterceptor>())).IsTrue();
        await Assert.That(ReferenceEquals(firstInterceptor, firstScope.ServiceProvider.GetRequiredService<CompositeStampInterceptor>())).IsTrue();
        await Assert.That(ReferenceEquals(firstInterceptor, secondWrapper.GetRequiredService<CompositeStampInterceptor>())).IsFalse();
    }

    [Test]
    public async Task Default_registration_rejects_resolving_the_scoped_interceptor_from_the_root_provider()
    {
        using var provider = createProvider();
        var wrapper = provider.GetRequiredService<IDbProviderServiceProvider>();

        await Assert.That(() => wrapper.GetRequiredService<CompositeStampInterceptor>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Default_registration_uses_the_current_user_from_each_context_scope()
    {
        using var provider = createProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        firstScope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = firstUserId;
        secondScope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = secondUserId;

        var firstEntity = new StampedEntity();
        var secondEntity = new StampedEntity();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<StampingDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<StampingDbContext>();

        firstContext.Add(firstEntity);
        secondContext.Add(secondEntity);
        await firstContext.SaveChangesAsync();
        await secondContext.SaveChangesAsync();

        await Assert.That(firstEntity.CreatedBy).IsEqualTo(firstUserId);
        await Assert.That(firstEntity.ModifiedBy).IsEqualTo(firstUserId);
        await Assert.That(secondEntity.CreatedBy).IsEqualTo(secondUserId);
        await Assert.That(secondEntity.ModifiedBy).IsEqualTo(secondUserId);
    }

    [Test]
    public async Task Default_registration_does_not_require_a_current_user()
    {
        using var provider = createProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StampingDbContext>();
        var entity = new StampedEntity();

        context.Add(entity);
        await context.SaveChangesAsync();

        await Assert.That(entity.CreatedAt).IsNotEqualTo(default(DateTimeOffset));
        await Assert.That(entity.CreatedBy).IsEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Default_registration_stamps_mixed_clr_and_shadow_key_types_without_extra_registration()
    {
        using var provider = createProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var guid = Guid.NewGuid();
        services.GetRequiredService<IWritableUserContext<Guid>>().UserId = guid;
        services.GetRequiredService<IWritableUserContext<string>>().UserId = "creator";
        services.GetRequiredService<IWritableUserContext<int>>().UserId = 42;
        var context = services.GetRequiredService<StampingDbContext>();
        var guidEntity = new StampedEntity();
        var stringEntity = new StringStampedEntity();
        var shadowEntity = new ShadowStampedEntity();
        context.AddRange(guidEntity, stringEntity, shadowEntity);

        context.SaveChanges();

        await Assert.That(guidEntity.CreatedBy).IsEqualTo(guid);
        await Assert.That(stringEntity.CreatedBy).IsEqualTo("creator");
        await Assert.That(stringEntity.ModifiedBy).IsEqualTo("creator");
        await Assert.That(context.Entry(shadowEntity).Property<int>("CreatedBy").CurrentValue).IsEqualTo(42);
        await Assert.That(context.Entry(shadowEntity).Property<int>("ModifiedBy").CurrentValue).IsEqualTo(42);

        services.GetRequiredService<IWritableUserContext<string>>().UserId = "modifier";
        stringEntity.Name = "Updated";
        await context.SaveChangesAsync();

        await Assert.That(stringEntity.CreatedBy).IsEqualTo("creator");
        await Assert.That(stringEntity.ModifiedBy).IsEqualTo("modifier");
    }

    [Test]
    public async Task Default_registration_keeps_string_users_isolated_between_scopes()
    {
        using var provider = createProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        firstScope.ServiceProvider.GetRequiredService<IWritableUserContext<string>>().UserId = "first";
        var firstContext = firstScope.ServiceProvider.GetRequiredService<StampingDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<StampingDbContext>();
        var firstEntity = new StringStampedEntity();
        var secondEntity = new StringStampedEntity { CreatedBy = "explicit", ModifiedBy = "explicit" };
        firstContext.Add(firstEntity);
        secondContext.Add(secondEntity);

        await firstContext.SaveChangesAsync();
        await secondContext.SaveChangesAsync();

        await Assert.That(firstEntity.CreatedBy).IsEqualTo("first");
        await Assert.That(secondEntity.CreatedBy).IsEqualTo("explicit");
        await Assert.That(secondEntity.ModifiedBy).IsEqualTo("explicit");
    }

    [Test]
    public async Task Dispatcher_uses_a_custom_closed_handler_once_per_save()
    {
        using var provider = createProvider(services => services.AddScoped<IUserStampHandler<string>, CustomStringHandler>());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StampingDbContext>();
        context.Add(new StringStampedEntity());
        await context.SaveChangesAsync();

        var handler = (CustomStringHandler)scope.ServiceProvider.GetRequiredService<IUserStampHandler<string>>();
        await Assert.That(handler.InvocationCount).IsEqualTo(1);
    }

    [Test]
    public async Task Design_time_factory_builds_the_model_without_application_user_services()
    {
        var factory = new DesignTimeFactory();
        using var context = factory.CreateDbContext([]);

        await Assert.That(context.Model.FindEntityType(typeof(StampedEntity))).IsNotNull();
        await Assert.That(context.IsDesignTime).IsTrue();
    }

    [Test]
    public async Task Design_time_factory_preserves_explicit_stamp_values_when_saving()
    {
        var factory = new DesignTimeFactory();
        using var context = factory.CreateDbContext([]);
        var timestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var entity = new StampedEntity
        {
            CreatedAt = timestamp,
            ModifiedAt = timestamp,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Add(entity);

        await context.SaveChangesAsync();

        await Assert.That(entity.CreatedAt).IsEqualTo(timestamp);
        await Assert.That(entity.ModifiedAt).IsEqualTo(timestamp);
        await Assert.That(entity.CreatedBy).IsEqualTo(userId);
        await Assert.That(entity.ModifiedBy).IsEqualTo(userId);
    }

    private static ServiceProvider createProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        Basilisque.DataAccess.EntityFramework.Base.IServiceCollectionExtensions.RegisterServices(services);
        services.AddSingleton<IConfiguration>(createConfiguration());
        services.AddSingleton<IDbContextOptionsConfigurator, InMemoryConfigurator>();
        services.AddDbContext<StampingDbContext>();
        configureServices?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static IConfiguration createConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "InMemory" })
            .Build();
    }
}
