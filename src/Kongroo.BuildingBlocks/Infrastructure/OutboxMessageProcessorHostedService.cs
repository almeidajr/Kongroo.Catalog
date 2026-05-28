using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxMessageProcessorHostedService<TDbContext>(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxMessageProcessorHostedService<TDbContext>> logger,
    TimeProvider timeProvider,
    IOptions<OutboxProcessingOptions> options
) : BackgroundService
    where TDbContext : OutboxDbContext<TDbContext>, IRelationalDbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox processor hosted service started for db context {DbContextType}.",
            typeof(TDbContext).Name
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxMessageProcessor<TDbContext>>();

                await processor.ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Unexpected error while processing outbox messages for db context {DbContextType}.",
                    typeof(TDbContext).Name
                );
            }

            await Task.Delay(options.Value.PollingInterval, timeProvider, stoppingToken);
        }
    }
}
