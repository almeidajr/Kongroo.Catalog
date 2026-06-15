using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : OutboxDbContext<CatalogDbContext>(options),
        IRelationalDbContext
{
    public static string Schema => "catalog";

    public DbSet<Game> Games => Set<Game>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<GameOwnership> GameOwnerships => Set<GameOwnership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new GameOwnershipConfiguration());
    }
}
