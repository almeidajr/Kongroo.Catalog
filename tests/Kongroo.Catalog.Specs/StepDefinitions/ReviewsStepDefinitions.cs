using Kongroo.Catalog.Specs.Drivers;
using Kongroo.Catalog.Specs.Support;
using Reqnroll;
using Shouldly;

namespace Kongroo.Catalog.Specs.StepDefinitions;

[Binding]
public sealed class ReviewsStepDefinitions(CatalogApiDriver catalogApiDriver, ApiScenarioContext scenarioContext)
{
    private sealed record CreatedGame(Guid Id);

    private sealed record ReviewsSummary(int Count, double? AverageRating);

    [Given("a game exists")]
    public async Task GivenAGameExists()
    {
        var playerId = Guid.NewGuid();
        scenarioContext.Authenticate(SpecsJwt.CreateToken(Guid.NewGuid(), "kongroo-admin", "Admin"));
        await catalogApiDriver.CreateGameAsync("Portal", "A puzzle platformer.", 19.99m);
        var created = await scenarioContext.LastResponse.ShouldNotBeNull().Content.ReadFromJsonAsync<CreatedGame>();
        scenarioContext.GameId = created.ShouldNotBeNull().Id;
        scenarioContext.Authenticate(SpecsJwt.CreateToken(playerId, "kongroo-player", "User"));
    }

    [When("the player submits a {int}-star review")]
    public async Task WhenThePlayerSubmitsAStarReview(int rating) =>
        await catalogApiDriver.SubmitReviewAsync(scenarioContext.GameId, rating, "Great.");

    [When("the game reviews are requested")]
    public async Task WhenTheGameReviewsAreRequested() =>
        await catalogApiDriver.RequestGameReviewsAsync(scenarioContext.GameId);

    [Then("the reviews summary shows {int} review with average {double}")]
    public async Task ThenTheReviewsSummaryShows(int count, double average)
    {
        var summary = await scenarioContext.LastResponse.ShouldNotBeNull().Content.ReadFromJsonAsync<ReviewsSummary>();
        summary.ShouldNotBeNull();
        summary.Count.ShouldBe(count);
        summary.AverageRating.ShouldBe(average);
    }
}
