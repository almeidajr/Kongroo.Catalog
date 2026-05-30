using Reqnroll;

namespace Kongroo.Catalog.Specs.Support;

[Binding]
public sealed class SpecsHooks(ApiScenarioContext apiScenarioContext)
{
    [BeforeTestRun]
    public static async Task BeforeTestRunAsync() => await SpecsEnvironment.StartAsync();

    [BeforeScenario("@webapi")]
    public async Task BeforeScenarioAsync() => await SpecsEnvironment.ResetAsync();

    [AfterScenario("@webapi")]
    public void AfterScenario() => apiScenarioContext.Dispose();

    [AfterTestRun]
    public static async Task AfterTestRunAsync() => await SpecsEnvironment.StopAsync();
}
