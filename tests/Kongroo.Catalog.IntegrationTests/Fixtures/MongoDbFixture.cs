using Testcontainers.MongoDb;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Kongroo.Catalog.IntegrationTests.Fixtures;

public sealed class MongoDbFixture(IMessageSink messageSink)
    : ContainerFixture<MongoDbBuilder, MongoDbContainer>(messageSink)
{
    protected override MongoDbBuilder Configure() =>
        new(Environment.GetEnvironmentVariable("MONGO_TEST_IMAGE") ?? "mongo:8.0");
}
