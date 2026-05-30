using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Infrastructure;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : OutboxDbContext<LibraryDbContext>(options),
        IRelationalDbContext
{
    public static string Schema => "library";

    public DbSet<GameOwnership> GameOwnerships => Set<GameOwnership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new GameOwnershipConfiguration());
    }
}
