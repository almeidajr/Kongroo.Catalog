using Testcontainers.Redis;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Kongroo.Catalog.IntegrationTests.Fixtures;

public sealed class RedisFixture(IMessageSink messageSink) : ContainerFixture<RedisBuilder, RedisContainer>(messageSink)
{
    protected override RedisBuilder Configure() => new("redis:8.2-alpine");
}
