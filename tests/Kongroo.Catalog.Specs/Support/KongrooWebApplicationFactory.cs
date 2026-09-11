using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kongroo.Catalog.Specs.Support;

public sealed class KongrooWebApplicationFactory(
    string databaseConnectionString,
    string rabbitMqHost,
    int rabbitMqPort,
    string rabbitMqUsername,
    string rabbitMqPassword,
    string mongoConnectionString,
    string redisConnectionString
) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                var testConfiguration = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = databaseConnectionString,
                    ["RabbitMq:Host"] = rabbitMqHost,
                    ["RabbitMq:Port"] = rabbitMqPort.ToString(CultureInfo.InvariantCulture),
                    ["RabbitMq:User"] = rabbitMqUsername,
                    ["RabbitMq:Pass"] = rabbitMqPassword,
                    ["Mongo:ConnectionString"] = mongoConnectionString,
                    ["Mongo:Database"] = "kongroo_catalog_specs",
                    ["ConnectionStrings:Redis"] = redisConnectionString,
                    ["Jwt:Issuer"] = SpecsJwt.Issuer,
                    ["Jwt:Audience"] = SpecsJwt.Audience,
                    ["Jwt:SigningKey"] = SpecsJwt.SigningKey,
                    ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                };

                configurationBuilder.AddInMemoryCollection(testConfiguration);
            }
        );

        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
    }
}
