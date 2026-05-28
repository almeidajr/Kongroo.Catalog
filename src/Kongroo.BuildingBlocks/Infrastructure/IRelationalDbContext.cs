namespace Kongroo.BuildingBlocks.Infrastructure;

public interface IRelationalDbContext
{
    static abstract string Schema { get; }
}
