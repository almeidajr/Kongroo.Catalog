using System.Net;
using Kongroo.Catalog.Specs.Drivers;
using Kongroo.Catalog.Specs.Support;
using Reqnroll;
using Shouldly;

namespace Kongroo.Catalog.Specs.StepDefinitions;

[Binding]
public sealed class SearchStepDefinitions(CatalogApiDriver catalogApiDriver, ApiScenarioContext scenarioContext)
{
    private const string Description = "Tend a haunted orchard by moonlight.";
    private const decimal PriceAmount = 24.90m;
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private sealed record CreatedGame(Guid Id);

    private sealed record SearchHit(Guid Id);

    [Given("a published game titled {string}")]
    public async Task GivenAPublishedGameTitled(string title)
    {
        scenarioContext.Authenticate(SpecsJwt.CreateToken(Guid.NewGuid(), "kongroo-admin", "Admin"));
        await catalogApiDriver.CreateGameAsync(title, Description, PriceAmount);
        var created = await scenarioContext.LastResponse.ShouldNotBeNull().Content.ReadFromJsonAsync<CreatedGame>();
        scenarioContext.GameId = created.ShouldNotBeNull().Id;

        await catalogApiDriver.PublishGameAsync(scenarioContext.GameId, title, Description, PriceAmount);
        scenarioContext.LastResponse.ShouldNotBeNull().StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Then("searching for {string} eventually returns the game as the first hit")]
    public async Task ThenSearchingEventuallyReturnsTheGameAsTheFirstHit(string query)
    {
        var deadline = DateTimeOffset.UtcNow + SearchTimeout;
        var firstHitId = await SearchFirstHitIdAsync(query);
        while (firstHitId != scenarioContext.GameId && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(PollInterval);
            firstHitId = await SearchFirstHitIdAsync(query);
        }

        firstHitId.ShouldBe(scenarioContext.GameId);
    }

    private async Task<Guid?> SearchFirstHitIdAsync(string query)
    {
        await catalogApiDriver.SearchAsync(query);
        var response = scenarioContext.LastResponse.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var hits = await response.Content.ReadFromJsonAsync<IReadOnlyList<SearchHit>>();

        return hits.ShouldNotBeNull() is [var firstHit, ..] ? firstHit.Id : null;
    }
}
