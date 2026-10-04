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

using Basilisque.DataAccess.EntityFramework.Base.Stamping;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.DataAccess.EntityFramework.Base.Unit.Tests.Stamping;

public class ShadowStampingTests
{
    private sealed class ShadowStampedEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class StampingDbContext : DbContext
    {
        public StampingDbContext(DbContextOptions<StampingDbContext> options) : base(options)
        { }

        public DbSet<ShadowStampedEntity> Entities => Set<ShadowStampedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShadowStampedEntity>()
                .UseShadowCreationStamp<ShadowStampedEntity, Guid>()
                .UseShadowModificationStamp<ShadowStampedEntity, Guid>();
        }
    }

    [Test]
    public async Task SaveChanges_stamps_shadow_properties()
    {
        var userId = Guid.NewGuid();
        var options = createOptions(userId);
        var entity = new ShadowStampedEntity { Name = "First" };

        await using var context = new StampingDbContext(options);
        context.Add(entity);

        await context.SaveChangesAsync();

        await Assert.That(context.Entry(entity).Property<DateTimeOffset>("CreatedAt").CurrentValue).IsNotEqualTo(default(DateTimeOffset));
        await Assert.That(context.Entry(entity).Property<Guid>("CreatedBy").CurrentValue).IsEqualTo(userId);
        await Assert.That(context.Entry(entity).Property<DateTimeOffset>("ModifiedAt").CurrentValue).IsEqualTo(context.Entry(entity).Property<DateTimeOffset>("CreatedAt").CurrentValue);
        await Assert.That(context.Entry(entity).Property<Guid>("ModifiedBy").CurrentValue).IsEqualTo(userId);
    }

    [Test]
    public async Task SaveChanges_does_not_set_user_stamps_when_no_current_user_is_available()
    {
        var options = createOptions(Guid.Empty);
        var entity = new ShadowStampedEntity { Name = "First" };

        await using var context = new StampingDbContext(options);
        context.Add(entity);

        await context.SaveChangesAsync();

        await Assert.That(context.Entry(entity).Property<Guid>("CreatedBy").CurrentValue).IsEqualTo(Guid.Empty);
        await Assert.That(context.Entry(entity).Property<Guid>("ModifiedBy").CurrentValue).IsEqualTo(Guid.Empty);
    }

    [Test]
    public async Task SaveChanges_respects_nested_suppression_scopes()
    {
        var options = createOptions(Guid.NewGuid());
        var entity = new ShadowStampedEntity { Name = "First" };

        using (StampSuppressor.Suppress())
        {
            using (StampSuppressor.Suppress())
            {
                await using var context = new StampingDbContext(options);
                context.Add(entity);
                await context.SaveChangesAsync();
            }

            await Assert.That(StampSuppressor.IsSuppressed).IsTrue();
        }

        await Assert.That(StampSuppressor.IsSuppressed).IsFalse();
        await Assert.That(entity.Id).IsNotEqualTo(Guid.Empty);

        await using var verificationContext = new StampingDbContext(options);
        var storedEntity = await verificationContext.Entities.SingleAsync();

        await Assert.That(verificationContext.Entry(storedEntity).Property<DateTimeOffset>("CreatedAt").CurrentValue).IsEqualTo(default(DateTimeOffset));
        await Assert.That(verificationContext.Entry(storedEntity).Property<Guid>("CreatedBy").CurrentValue).IsEqualTo(Guid.Empty);
    }

    private static DbContextOptions<StampingDbContext> createOptions(Guid userId)
    {
        var userContext = new Basilisque.Core.Auth.WritableUserContext<Guid> { UserId = userId };
        var handlers = new IStampHandler[] { new TimestampStampHandler(), new UserStampHandler<Guid>(userContext) };
        var interceptor = new CompositeStampInterceptor(handlers);

        return new DbContextOptionsBuilder<StampingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptor)
            .Options;
    }
}
