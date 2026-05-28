using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kongroo.BuildingBlocks.Presentation.Attributes;
using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Presentation;

public sealed record UpdateGameRequest(
    [property: Required]
    [property: MinLength(GameTitle.MinLength)]
    [property: MaxLength(GameTitle.MaxLength)]
    [property: Description("Display title of the game.")]
        string Title,
    [property: Required]
    [property: MinLength(GameDescription.MinLength)]
    [property: MaxLength(GameDescription.MaxLength)]
    [property: Description("Detailed summary of the game.")]
        string Description,
    [property: Required]
    [property: NonNegative<decimal>]
    [property: Description("Current game price amount.")]
        decimal PriceAmount,
    [property: Description("Current game price currency code.")] Currency Currency,
    [property: Description("Current publishing status of the game.")] GameStatus Status
);

