using Kongroo.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kongroo.BuildingBlocks.Infrastructure;

public class DbInitializer<TDbContext>(IHostEnvironment environment, TDbContext context) : IApplicationInitializer
    where TDbContext : DbContext
{
    public int Priority => 0;

    public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(environment.IsDevelopment());

    public async Task InitializeAsync(CancellationToken cancellationToken) =>
        await context.Database.MigrateAsync(cancellationToken);
}
