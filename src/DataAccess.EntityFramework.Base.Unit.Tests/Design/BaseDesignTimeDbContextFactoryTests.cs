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

using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Design;

public class BaseDesignTimeDbContextFactoryTests
{
    private sealed class Resource : IDesignTimeServiceLifetime
    {
        public int DisposeCount { get; private set; }
        public int AsyncDisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ScopedResource : IDisposable, IAsyncDisposable
    {
        public Resource Resource { get; } = new();
        public void Dispose() => Resource.Dispose();
        public ValueTask DisposeAsync() => Resource.DisposeAsync();
    }

    private sealed class AsyncOnlyResource : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            IsDisposed = true;
        }
    }

    private sealed class FakeDesignContext : BaseDbContext<FakeDesignContext>
    {
        public IServiceProvider Services { get; }
        public ScopedResource Scoped { get; }
        public Resource Singleton { get; }
        public string? Argument { get; }

        public FakeDesignContext(IDbProviderServiceProvider provider, IServiceProvider services,
            ScopedResource scoped, Resource singleton, IConfiguration configuration) : base(provider)
        {
            Services = services;
            Scoped = scoped;
            Singleton = singleton;
            Argument = configuration["Argument"];
        }
    }

    private class TestFactory : BaseDesignTimeDbContextFactory<FakeDesignContext>
    {
        public FakeDesignContext? LastContext { get; private set; }
        public bool FailCreation { get; init; }
        public AsyncOnlyResource? AsyncResource { get; private set; }

        protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Argument"] = args.FirstOrDefault() }).Build();
        }

        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddScoped<ScopedResource>();
            services.AddSingleton<Resource>();
            services.AddScoped<AsyncOnlyResource>();
        }

        protected override FakeDesignContext CreateDbContext(IServiceProvider serviceProvider, string[] args)
        {
            LastContext = base.CreateDbContext(serviceProvider, args);

            if (FailCreation)
            {
                AsyncResource = serviceProvider.GetRequiredService<AsyncOnlyResource>();
                throw new InvalidOperationException("Context creation failed.");
            }

            return LastContext;
        }
    }

    private sealed class UnsupportedContext : DbContext
    {
        public UnsupportedContext(ScopedResource resource)
        { }
    }

    private sealed class UnsupportedFactory : BaseDesignTimeDbContextFactory<UnsupportedContext>
    {
        public ScopedResource Resource { get; } = new();

        protected override void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddScoped(_ => Resource);
        }
    }

    [Test]
    public async Task CreateDbContext_sets_IsDesignTime_true()
    {
        var factory = new TestFactory();

        using var ctx = factory.CreateDbContext([]);

        await Assert.That(ctx.IsDesignTime).IsTrue();
    }

    [Test]
    public async Task CreateDbContext_isolates_arguments_and_services_for_each_call()
    {
        var factory = new TestFactory();
        using var first = factory.CreateDbContext(["first"]);
        using var second = factory.CreateDbContext(["second"]);
        using var third = new TestFactory().CreateDbContext(["third"]);

        await Assert.That(first.Argument).IsEqualTo("first");
        await Assert.That(second.Argument).IsEqualTo("second");
        await Assert.That(third.Argument).IsEqualTo("third");
        await Assert.That(first.Scoped).IsNotSameReferenceAs(second.Scoped);
        await Assert.That(second.Scoped).IsNotSameReferenceAs(third.Scoped);
        await Assert.That(first.Singleton).IsNotSameReferenceAs(second.Singleton);

        first.Dispose();

        await Assert.That(first.Scoped.Resource.DisposeCount).IsEqualTo(1);
        await Assert.That(first.Singleton.DisposeCount).IsEqualTo(1);
        await Assert.That(second.Scoped.Resource.DisposeCount).IsEqualTo(0);
        await Assert.That(second.Singleton.DisposeCount).IsEqualTo(0);
        await Assert.That(second.Services.GetRequiredService<ScopedResource>()).IsSameReferenceAs(second.Scoped);
    }

    [Test]
    public async Task Dispose_releases_scope_and_provider_exactly_once()
    {
        var context = new TestFactory().CreateDbContext([]);
        context.Dispose();
        context.Dispose();
        await context.DisposeAsync();

        await Assert.That(context.Scoped.Resource.DisposeCount).IsEqualTo(1);
        await Assert.That(context.Singleton.DisposeCount).IsEqualTo(1);
        await Assert.That(context.Scoped.Resource.AsyncDisposeCount).IsEqualTo(0);
        await Assert.That(() => context.Services.GetRequiredService<ScopedResource>()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task DisposeAsync_releases_async_only_services_and_provider_exactly_once()
    {
        var context = new TestFactory().CreateDbContext([]);
        var asyncOnly = context.Services.GetRequiredService<AsyncOnlyResource>();

        await context.DisposeAsync();
        await context.DisposeAsync();
        context.Dispose();

        await Assert.That(asyncOnly.IsDisposed).IsTrue();
        await Assert.That(context.Scoped.Resource.AsyncDisposeCount).IsEqualTo(1);
        await Assert.That(context.Singleton.AsyncDisposeCount).IsEqualTo(1);
        await Assert.That(context.Singleton.DisposeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Failed_creation_releases_scope_and_provider()
    {
        var factory = new TestFactory { FailCreation = true };

        await Assert.That(() => factory.CreateDbContext([])).Throws<InvalidOperationException>();

        await Assert.That(factory.LastContext!.Scoped.Resource.AsyncDisposeCount).IsEqualTo(1);
        await Assert.That(factory.LastContext.Singleton.AsyncDisposeCount).IsEqualTo(1);
        await Assert.That(factory.AsyncResource!.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Context_without_lifetime_contract_is_rejected_and_its_scope_is_disposed()
    {
        var factory = new UnsupportedFactory();

        await Assert.That(() => factory.CreateDbContext([])).Throws<InvalidOperationException>();

        await Assert.That(factory.Resource.Resource.AsyncDisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Lifetime_attachment_rejects_duplicates_and_disposed_contexts()
    {
        using var context = new TestFactory().CreateDbContext([]);
        using var lifetime = new Resource();
        var owner = (IDbContextDesignTimeLifetimeOwner)context;

        await Assert.That(() => owner.SetDesignTimeServiceLifetime(lifetime)).Throws<InvalidOperationException>();
        await Assert.That(lifetime.DisposeCount).IsEqualTo(0);

        context.Dispose();

        await Assert.That(() => owner.SetDesignTimeServiceLifetime(lifetime)).Throws<ObjectDisposedException>();
    }
}
