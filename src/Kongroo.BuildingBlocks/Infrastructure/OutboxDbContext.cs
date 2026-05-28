using Microsoft.EntityFrameworkCore;

namespace Kongroo.BuildingBlocks.Infrastructure;

public abstract class OutboxDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
}

public abstract class OutboxDbContext<TDbContext>(DbContextOptions options) : OutboxDbContext(options)
    where TDbContext : OutboxDbContext<TDbContext>, IRelationalDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(TDbContext.Schema);
        base.OnModelCreating(modelBuilder);
    }
}
