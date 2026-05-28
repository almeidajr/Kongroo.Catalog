using System.ComponentModel;
using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed record GetOrderLineResponse(
    [property: Description("Unique identifier of the purchased game.")] Guid GameId,
    [property: Description("Snapshot of the purchased game title.")] string GameTitle,
    [property: Description("Snapshot of the list price amount before discounts.")] decimal ListPriceAmount,
    [property: Description("Currency used by the list price amount.")] Currency ListPriceCurrency,
    [property: Description("Snapshot of the final price amount charged for the game.")] decimal FinalPriceAmount,
    [property: Description("Currency used by the final price amount.")] Currency FinalPriceCurrency,
    [property: Description("Promotion applied to the line when one was active during purchase.")]
        Guid? AppliedPromotionId
);

