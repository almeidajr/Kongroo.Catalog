using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Presentation.Requests;

public sealed record PlaceOrderRequest(
    [property: Required]
    [property: MinLength(1)]
    [property: Description("Unique identifiers of the games to purchase in a single order.")]
        Guid[] GameIds
);
