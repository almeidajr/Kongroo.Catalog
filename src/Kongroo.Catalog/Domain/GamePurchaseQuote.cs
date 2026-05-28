namespace Kongroo.Catalog.Domain;

public sealed record GamePurchaseQuote(
    GameId GameId,
    GameTitle GameTitle,
    Money ListPrice,
    Money FinalPrice,
    PromotionId? AppliedPromotionId
);

