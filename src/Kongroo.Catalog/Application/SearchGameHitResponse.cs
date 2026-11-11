namespace Kongroo.Catalog.Application;

public sealed record SearchGameHitResponse(
    Guid Id,
    string Title,
    string Description,
    decimal PriceAmount,
    string Currency,
    double Score
);
