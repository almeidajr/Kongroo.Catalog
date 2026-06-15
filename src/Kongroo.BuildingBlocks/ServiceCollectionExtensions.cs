using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kongroo.BuildingBlocks;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBuildingBlocks(IConfiguration configuration)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<OutboxMessagesInterceptor>();

            services
                .AddOptions<OutboxProcessingOptions>()
                .Bind(configuration.GetRequiredSection(OutboxProcessingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHostedService<ApplicationInitializationService>();

            return services;
        }

        public IServiceCollection AddOutboxDbContext<TDbContext>(IConfiguration configuration)
            where TDbContext : OutboxDbContext<TDbContext>, IRelationalDbContext
        {
            services.AddRelationalDbContext<TDbContext>(configuration);

            services.AddScoped<OutboxMessageProcessor<TDbContext>>();
            services.AddHostedService<OutboxMessageProcessorHostedService<TDbContext>>();

            return services;
        }

        public IServiceCollection AddRelationalDbContext<TDbContext>(IConfiguration configuration)
            where TDbContext : DbContext, IRelationalDbContext
        {
            services.AddDbContext<TDbContext>(
                (serviceProvider, contextOptions) =>
                {
                    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

                    contextOptions
                        .AddInterceptors(serviceProvider.GetRequiredService<OutboxMessagesInterceptor>())
                        .UseNpgsql(
                            configuration.GetConnectionString("Database"),
                            postgresOptions => postgresOptions.MigrationsHistoryTable("migrations", TDbContext.Schema)
                        )
                        .UseSnakeCaseNamingConvention();

                    if (environment.IsDevelopment())
                    {
                        contextOptions.EnableDetailedErrors().EnableSensitiveDataLogging();
                    }
                }
            );
            services.AddApplicationInitializer<DbInitializer<TDbContext>>();
            return services;
        }

        public IServiceCollection AddApplicationInitializer<TApplicationInitializer>()
            where TApplicationInitializer : class, IApplicationInitializer =>
            services.AddScoped<IApplicationInitializer, TApplicationInitializer>();
    }
}
