using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Kongroo.Catalog.IntegrationTests.Fixtures;

/// <summary>In-memory HybridCache (no Redis) for handler tests that only need a cache instance.</summary>
internal static class TestCache
{
    public static HybridCache Create() =>
        new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>();
}
