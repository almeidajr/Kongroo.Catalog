namespace Kongroo.Catalog.Application;

public sealed record PlaceOrderCommand(Guid CustomerId, string Email, string CustomerName, IReadOnlyList<Guid> GameIds);
