namespace Kongroo.Catalog.Application;

public sealed record ApplyPaymentResultCommand(Guid OrderId, bool IsApproved, DateTimeOffset ProcessedAt);
