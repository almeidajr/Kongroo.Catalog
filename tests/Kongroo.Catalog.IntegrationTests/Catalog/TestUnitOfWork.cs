using Kongroo.BuildingBlocks.Application;
using Kongroo.Catalog.Infrastructure;

namespace Kongroo.Catalog.IntegrationTests.Catalog;

internal sealed class TestUnitOfWork(CatalogDbContext context) : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
