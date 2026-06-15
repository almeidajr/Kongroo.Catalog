namespace Kongroo.Catalog.Application;

public sealed record GetOrderQuery(Guid CustomerId, Guid OrderId);
