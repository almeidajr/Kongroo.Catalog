using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

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
                .AddOptions<RabbitMqTransportOptions>()
                .Bind(configuration.GetRequiredSection("RabbitMq"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddMassTransit(busRegistration =>
            {
                busRegistration.SetKebabCaseEndpointNameFormatter();

                busRegistration.AddEntityFrameworkOutbox<CatalogDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();
                });

                busRegistration.AddConsumer<PaymentProcessedIntegrationEventConsumer>();

                busRegistration.UsingRabbitMq((context, busFactory) => busFactory.ConfigureEndpoints(context));
            });
        }
    }
}
