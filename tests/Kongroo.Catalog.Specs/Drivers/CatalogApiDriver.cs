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

    public async Task CreateGameAsync(string title, string description, decimal priceAmount)
    {
        var response = await scenarioContext.Client.PostAsJsonAsync(
            "/games",
            new
            {
                title,
                description,
                priceAmount,
                currency = "USD",
            }
        );
        scenarioContext.SetLastResponse(response);
    }

    public async Task SubmitReviewAsync(Guid gameId, int rating, string? text)
    {
        var response = await scenarioContext.Client.PostAsJsonAsync($"/games/{gameId}/reviews", new { rating, text });
        scenarioContext.SetLastResponse(response);
    }

    public async Task RequestGameReviewsAsync(Guid gameId)
    {
        var response = await scenarioContext.Client.GetAsync($"/games/{gameId}/reviews");
        scenarioContext.SetLastResponse(response);
    }
}
