using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Infrastructure;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    [Required]
    public Uri? Uri { get; init; }

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
