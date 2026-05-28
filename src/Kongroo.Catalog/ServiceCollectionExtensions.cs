using Kongroo.BuildingBlocks;
using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Contracts;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddScoped<IDomainEventHandler, OrderPlacedDomainEventHandler>();

            services.AddScoped<AcquireGameOwnershipCommandHandler>();
            services.AddScoped<GetGameOwnershipQueryHandler>();
            services.AddScoped<GetGameOwnershipsQueryHandler>();
            services.AddScoped<
                IIntegrationEventHandler<OrderCompletedIntegrationEvent>,
                OrderCompletedIntegrationEventHandler
            >();
        }

        private void AddInfrastructure(IConfiguration configuration)
        {
            services.AddOutboxDbContext<CatalogDbContext>(configuration);
            services.AddOutboxDbContext<LibraryDbContext>(configuration);
        }
    }
}
