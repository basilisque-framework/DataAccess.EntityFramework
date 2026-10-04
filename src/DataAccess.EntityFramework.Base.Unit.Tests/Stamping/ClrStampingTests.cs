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

public class ClrStampingTests
{
    private sealed class StampedEntity : IStampChanges
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
    }

    private sealed class StampingDbContext : DbContext
    {
        public StampingDbContext(DbContextOptions<StampingDbContext> options) : base(options)
        { }

        public DbSet<StampedEntity> Entities => Set<StampedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyStampConfigurations();
        }
    }

    [Test]
    public async Task SaveChanges_stamps_clr_creation_and_modification_properties()
    {
        var userId = Guid.NewGuid();
        var options = createOptions(userId);
        var entity = new StampedEntity { Name = "First" };

        await using var context = new StampingDbContext(options);
        context.Add(entity);

        await context.SaveChangesAsync();

        await Assert.That(entity.CreatedAt).IsNotEqualTo(default(DateTimeOffset));
        await Assert.That(entity.CreatedAt.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(entity.CreatedBy).IsEqualTo(userId);
        await Assert.That(entity.ModifiedAt).IsEqualTo(entity.CreatedAt);
        await Assert.That(entity.ModifiedBy).IsEqualTo(userId);
    }

    [Test]
    public async Task SaveChanges_stamps_modification_but_preserves_creation_properties()
    {
        var userId = Guid.NewGuid();
        var options = createOptions(userId);
        var entity = new StampedEntity { Name = "First" };

        await using (var context = new StampingDbContext(options))
        {
            context.Add(entity);
            await context.SaveChangesAsync();
        }

        var createdAt = entity.CreatedAt;
        entity.Name = "Updated";
        entity.CreatedAt = createdAt.AddDays(-1);
        entity.CreatedBy = Guid.NewGuid();

        await using (var context = new StampingDbContext(options))
        {
            context.Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        await using var verificationContext = new StampingDbContext(options);
        var storedEntity = await verificationContext.Entities.SingleAsync();

        await Assert.That(storedEntity.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(storedEntity.CreatedBy).IsEqualTo(userId);
        await Assert.That(storedEntity.ModifiedAt).IsGreaterThan(createdAt);
        await Assert.That(storedEntity.ModifiedBy).IsEqualTo(userId);
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
