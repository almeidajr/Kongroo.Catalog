using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Presentation.Requests;

public sealed record SearchGamesRequest(
    [property: Required] [property: Description("Words to find in game titles and descriptions.")] string? Q,
    [property: Range(1, SearchGamesRequest.MaxLimit)]
    [property: Description("Maximum number of hits, from 1 to 50. Defaults to 20.")]
        int? Limit
)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;
}
