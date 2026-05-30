namespace Kongroo.Catalog.Application;

public sealed record GetOrderQuery(Guid BuyerId, Guid OrderId);
