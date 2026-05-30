using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kongroo.Catalog.Specs.Support;

public sealed class ApiScenarioContext : IDisposable
{
    private HttpClient? _client;

    public HttpClient Client =>
        _client ??= SpecsEnvironment.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

    public HttpResponseMessage? LastResponse { get; private set; }

    public void SetLastResponse(HttpResponseMessage response)
    {
        LastResponse?.Dispose();
        LastResponse = response;
    }

    public void Authenticate(string accessToken) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public void Dispose()
    {
        LastResponse?.Dispose();
        _client?.Dispose();
    }
}
