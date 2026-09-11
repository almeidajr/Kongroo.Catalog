using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Presentation.Requests;

public sealed record SubmitReviewRequest(
    [property: Range(1, 5)]
    [property: Description("Rating from 1 (worst) to 5 (best).")]
        int Rating,
    [property: MaxLength(2000)]
    [property: Description("Optional free-text review, up to 2000 characters.")]
        string? Text
);
