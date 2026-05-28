namespace Kongroo.BuildingBlocks.Application;

public interface IApplicationInitializer
{
    int Priority { get; }

    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken);

    Task InitializeAsync(CancellationToken cancellationToken);
}
