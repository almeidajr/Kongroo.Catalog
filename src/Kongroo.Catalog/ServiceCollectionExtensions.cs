using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Contracts;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Payments.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Kongroo.Catalog;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCatalogModule(IConfiguration configuration)
        {
            services.AddValidation();
            services.AddApplication();
            services.AddInfrastructure(configuration);

            return services;
        }

        private void AddApplication()
        {
            services.AddScoped<CreatePromotionCommandHandler>();
            services.AddScoped<CreateGameCommandHandler>();
            services.AddScoped<GetGameQueryHandler>();
            services.AddScoped<GetOrderQueryHandler>();
            services.AddScoped<GetOrdersQueryHandler>();
            services.AddScoped<GetGamesQueryHandler>();
            services.AddScoped<PlaceOrderCommandHandler>();
            services.AddScoped<UpdateGameCommandHandler>();
            services.AddScoped<DeleteGameCommandHandler>();
            services.AddScoped<SubmitReviewCommandHandler>();
            services.AddScoped<GetGameReviewsQueryHandler>();
            services.AddScoped<SearchGamesQueryHandler>();

            services.AddScoped<ApplyPaymentResultCommandHandler>();

            services.AddDomainEventHandler<OrderPlacedDomainEventHandler>();

            services.AddScoped<GetOwnershipQueryHandler>();
            services.AddScoped<GetOwnershipsQueryHandler>();
        }

        private void AddInfrastructure(IConfiguration configuration)
        {
            services.AddSingleton(TimeProvider.System);

            services.AddRelationalDbContext<CatalogDbContext>(contextOptions =>
                contextOptions.UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    postgresOptions => postgresOptions.MigrationsHistoryTable("migrations", CatalogDbContext.Schema)
                )
            );
            services.AddDbInitializer<CatalogDbContext>();

            services
                .AddOptions<MongoOptions>()
                .Bind(configuration.GetRequiredSection(MongoOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IMongoClient>(provider => new MongoClient(
                provider.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString
            ));
            services.AddSingleton(provider =>
            {
                var mongoOptions = provider.GetRequiredService<IOptions<MongoOptions>>().Value;

                return provider
                    .GetRequiredService<IMongoClient>()
                    .GetDatabase(mongoOptions.Database)
                    .GetCollection<ReviewDocument>(ReviewDocument.CollectionName);
            });
            services.AddApplicationInitializer<ReviewIndexInitializer>();

            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = configuration.GetRequiredConnectionString("Redis");
                redisOptions.InstanceName = "catalog:";
            });
            services.AddHybridCache();
            services.AddOptions<HybridCacheOptions>().Bind(configuration.GetRequiredSection("HybridCache"));

            services.AddSearch(configuration);

            services.AddMessaging(configuration);
        }

        private void AddSearch(IConfiguration configuration)
        {
            services
                .AddOptions<GameSearchOptions>()
                .Bind(configuration.GetRequiredSection(GameSearchOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton(static provider =>
            {
                var gameSearchOptions = provider.GetRequiredService<IOptions<GameSearchOptions>>().Value;

                // Retries belong to the Polly pipelines; transport retries would multiply them, and a short timeout keeps a hung cluster from stretching their window.
                return new ElasticsearchClient(
                    new ElasticsearchClientSettings(gameSearchOptions.Uri!)
                        .Authentication(new BasicAuthentication(gameSearchOptions.Username, gameSearchOptions.Password))
                        .MaximumRetries(0)
                        .RequestTimeout(TimeSpan.FromSeconds(10))
                );
            });
            services.AddResiliencePipeline(
                GameSearchPipelines.Indexing,
                static pipeline =>
                    pipeline.AddRetry(
                        new RetryStrategyOptions
                        {
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            // Jittered delays add up to about two minutes (median) before the message goes to _error.
                            Delay = TimeSpan.FromSeconds(2),
                            MaxRetryAttempts = 7,
                        }
                    )
            );
            services.AddResiliencePipeline(
                GameSearchPipelines.IndexSetup,
                static pipeline =>
                    pipeline.AddRetry(
                        new RetryStrategyOptions
                        {
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            Delay = TimeSpan.FromSeconds(1),
                            MaxDelay = TimeSpan.FromSeconds(30),
                            MaxRetryAttempts = int.MaxValue,
                        }
                    )
            );
            services.AddHostedService<GameSearchIndexInitializer>();
        }

        private void AddMessaging(IConfiguration configuration)
        {
            var transport = configuration.GetValue("Messaging:Transport", MessagingTransport.RabbitMq);

            if (transport == MessagingTransport.RabbitMq)
            {
                services
                    .AddOptions<RabbitMqTransportOptions>()
                    .Bind(configuration.GetRequiredSection("RabbitMq"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            }

            services.AddMassTransit(busRegistration =>
            {
                busRegistration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("catalog"));

                busRegistration.AddEntityFrameworkOutbox<CatalogDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();
                });

                busRegistration.AddConsumer<PaymentProcessedIntegrationEventConsumer>();
                busRegistration.AddConsumer<GameIndexRequestedConsumer>();

                if (transport == MessagingTransport.AmazonSqs)
                {
                    var region = configuration.GetValue<string>("Aws:Region");
                    if (string.IsNullOrWhiteSpace(region))
                    {
                        throw new InvalidOperationException(
                            "Configuration value 'Aws:Region' is required when Messaging:Transport is AmazonSqs."
                        );
                    }

                    busRegistration.UsingAmazonSqs(
                        (context, busFactory) =>
                        {
                            // Credentials come from the AWS SDK default chain (AWS_ACCESS_KEY_ID,
                            // AWS_SECRET_ACCESS_KEY, AWS_SESSION_TOKEN) — nothing to configure here.
                            busFactory.Host(region, static _ => { });
                            busFactory.Message<OrderPlacedIntegrationEvent>(static message =>
                                message.SetEntityName(MessagingTopics.OrderPlaced)
                            );
                            busFactory.Message<PaymentProcessedIntegrationEvent>(static message =>
                                message.SetEntityName(MessagingTopics.PaymentProcessed)
                            );
                            busFactory.Message<GameIndexRequested>(static message =>
                                message.SetEntityName(MessagingTopics.GameIndexRequested)
                            );
                            busFactory.ConfigureEndpoints(context);
                        }
                    );
                }
                else
                {
                    busRegistration.UsingRabbitMq((context, busFactory) => busFactory.ConfigureEndpoints(context));
                }
            });
        }
    }
}
