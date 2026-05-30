using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Presentation;

public sealed record CreatePromotionRequest(
    [property: Required]
    [property: Range(typeof(decimal), "0", "100")]
    [property: Description("Percentage discount applied while the promotion is active.")]
        decimal Discount,
    [property: Description("Instant when the promotion becomes active.")] DateTimeOffset StartsAt,
    [property: Description("Instant when the promotion stops being active.")] DateTimeOffset EndsAt
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt >= EndsAt)
        {
            yield return new ValidationResult(
                "Promotion start must be earlier than end.",
                [nameof(StartsAt), nameof(EndsAt)]
            );
        }
    }
}
