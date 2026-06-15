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

    public DbSet<Ownership> Ownerships => Set<Ownership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OwnershipConfiguration());
    }
}
