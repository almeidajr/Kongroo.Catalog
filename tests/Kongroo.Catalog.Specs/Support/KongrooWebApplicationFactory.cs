using Microsoft.AspNetCore.Mvc.Testing;

namespace Kongroo.Catalog.Specs.Support;

public sealed class KongrooWebApplicationFactory(string databaseConnectionString) : WebApplicationFactory<Program>
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
                    ["Jwt:Issuer"] = SpecsJwt.Issuer,
                    ["Jwt:Audience"] = SpecsJwt.Audience,
                    ["Jwt:SigningKey"] = SpecsJwt.SigningKey,
                    ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                    ["OutboxProcessing:PollingInterval"] = "00:10:00",
                    ["OutboxProcessing:BatchSize"] = "1",
                };

                configurationBuilder.AddInMemoryCollection(testConfiguration);
            }
        );

        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
    }
}
