using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Common;
using ProjectX.Infrastructure.Persistance.Interceptors;

namespace ProjectX.UnitTests.Infrastructure;

public class AuditableEntityInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_SetsUtcModificationTimeForAddedAndModifiedEntities()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(timeProvider))
            .Options;

        await using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Name = "initial" };

        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(timeProvider.GetUtcNow(), entity.ModDate);
        Assert.Equal(TimeSpan.Zero, entity.ModDate.Offset);

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        entity.Name = "updated";
        await context.SaveChangesAsync();

        Assert.Equal(timeProvider.GetUtcNow(), entity.ModDate);
        Assert.Equal(TimeSpan.Zero, entity.ModDate.Offset);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestAuditableEntity> Entities => Set<TestAuditableEntity>();
    }

    private sealed class TestAuditableEntity : BaseAuditableEntity
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
