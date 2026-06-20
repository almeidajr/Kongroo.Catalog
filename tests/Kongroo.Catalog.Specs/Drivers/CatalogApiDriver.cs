using Kongroo.Catalog.Specs.Support;

namespace Kongroo.Catalog.Specs.Drivers;

public sealed class CatalogApiDriver(ApiScenarioContext scenarioContext)
{
    public async Task RequestHealthAsync()
    {
        var response = await scenarioContext.Client.GetAsync("/health");
        scenarioContext.SetLastResponse(response);
    }

    public async Task RequestGamesAsync()
    {
        var response = await scenarioContext.Client.GetAsync("/games");
        scenarioContext.SetLastResponse(response);
    }

    public async Task PlaceOrderAsync(Guid gameId)
    {
        var response = await scenarioContext.Client.PostAsJsonAsync("/orders", new { gameIds = new[] { gameId } });
        scenarioContext.SetLastResponse(response);
    }
}
