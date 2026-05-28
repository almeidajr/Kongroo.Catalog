namespace Kongroo.Catalog.Application;

public sealed record AcquireGameOwnershipCommand(Guid OwnerId, Guid GameId, Guid OrderId, DateTimeOffset AcquiredAt);

