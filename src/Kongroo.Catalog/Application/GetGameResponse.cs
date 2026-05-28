using System.ComponentModel;
using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed record GetGameResponse(
    [property: Description("Unique identifier assigned to the game.")] Guid Id,
    [property: Description("Display title of the game.")] string Title,
    [property: Description("Detailed summary of the game.")] string Description,
    [property: Description("Current game price amount.")] decimal PriceAmount,
    [property: Description("Current game price currency code.")] Currency Currency,
    [property: Description("Current publishing status of the game.")] GameStatus Status,
    [property: Description("Currently active promotion for the game, when one exists.")]
        GetPromotionResponse? ActivePromotion
);

