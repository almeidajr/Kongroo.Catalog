using Kongroo.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kongroo.BuildingBlocks.Infrastructure;

public class DbInitializer<TDbContext>(
    IHostEnvironment environment,
    TDbContext context,
    ILogger<DbInitializer<TDbContext>> logger
) : IApplicationInitializer
    where TDbContext : DbContext
{
    public int Priority => 0;

    public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken) => ValueTask.FromResult(true);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogWarning(
                "Auto-applying EF Core migrations in the {Environment} environment. "
                    + "This is a temporary measure; use a controlled migration process for production.",
                environment.EnvironmentName
            );
        }

        await context.Database.MigrateAsync(cancellationToken);
    }
}
