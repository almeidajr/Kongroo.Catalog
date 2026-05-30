using Kongroo.Catalog.Specs.Drivers;
using Kongroo.Catalog.Specs.Support;
using Reqnroll;
using Shouldly;

namespace Kongroo.Catalog.Specs.StepDefinitions;

[Binding]
public sealed class CatalogSmokeStepDefinitions(CatalogApiDriver catalogApiDriver, ApiScenarioContext scenarioContext)
{
    [Given("an authenticated player")]
    public void GivenAnAuthenticatedPlayer() =>
        scenarioContext.Authenticate(SpecsJwt.CreateToken(Guid.NewGuid(), "kongroo-player", "User"));

    [When("the health endpoint is requested")]
    public async Task WhenTheHealthEndpointIsRequested() => await catalogApiDriver.RequestHealthAsync();

    [When("the games catalog is requested")]
    public async Task WhenTheGamesCatalogIsRequested() => await catalogApiDriver.RequestGamesAsync();

    [Then("the response status code is {int}")]
    public void ThenTheResponseStatusCodeIs(int statusCode) =>
        ((int)scenarioContext.LastResponse.ShouldNotBeNull().StatusCode).ShouldBe(statusCode);
}
