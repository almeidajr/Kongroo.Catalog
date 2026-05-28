using System.Diagnostics;
using Kongroo.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kongroo.BuildingBlocks.Infrastructure;

public class ApplicationInitializationService(
    ILogger<ApplicationInitializationService> logger,
    IServiceScopeFactory scopeFactory
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var initializers = scope
            .ServiceProvider.GetServices<IApplicationInitializer>()
            .OrderBy(initializer => initializer.Priority);

        foreach (var initializer in initializers)
        {
            await ExecuteAsync(initializer, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ExecuteAsync(IApplicationInitializer initializer, CancellationToken cancellationToken)
    {
        var initializerName = initializer.GetType().ToDisplayName();
        var startingTimestamp = Stopwatch.GetTimestamp();
        if (!await initializer.IsEnabledAsync(cancellationToken))
        {
            logger.LogInformation(
                "Skipping application initializer {Initializer} with priority {Priority}",
                initializerName,
                initializer.Priority
            );
            return;
        }

        logger.LogInformation(
            "Running application initializer {Initializer} with priority {Priority}",
            initializerName,
            initializer.Priority
        );

        await initializer.InitializeAsync(cancellationToken);

        logger.LogInformation(
            "Completed application initializer {Initializer} with priority {Priority} in {ElapsedMilliseconds} ms",
            initializerName,
            initializer.Priority,
            Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds
        );
    }
}
