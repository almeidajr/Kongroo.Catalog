namespace Kongroo.Catalog.Application;

public sealed record PlaceOrderCommand(Guid BuyerId, IReadOnlyList<Guid> GameIds);
