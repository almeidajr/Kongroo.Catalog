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
            services.AddHybridCache(cacheOptions =>
                cacheOptions.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(1),
                }
            );

            services.AddMessaging(configuration);
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

                if (transport == MessagingTransport.AmazonSqs)
                {
                    var region =
                        configuration.GetValue<string>("Aws:Region")
                        ?? throw new InvalidOperationException(
                            "Configuration value 'Aws:Region' is required when Messaging:Transport is AmazonSqs."
                        );

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
