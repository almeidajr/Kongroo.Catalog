namespace Kongroo.Catalog.Application;

public sealed record PlaceOrderCommand(Guid CustomerId, IReadOnlyList<Guid> GameIds);
