using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using MongoDB.Driver;

namespace Kongroo.Catalog.IntegrationTests.Catalog;

public sealed class ReviewsTestCollection(MongoDbFixture fixture)
{
    private const string DatabaseName = "kongroo_catalog_tests";

    public IMongoCollection<ReviewDocument> Collection { get; } =
        new MongoClient(fixture.Container.GetConnectionString())
            .GetDatabase(DatabaseName)
            .GetCollection<ReviewDocument>(ReviewDocument.CollectionName);

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await Collection.Database.DropCollectionAsync(ReviewDocument.CollectionName, cancellationToken);
        await new ReviewIndexInitializer(Collection).InitializeAsync(cancellationToken);
    }
}
