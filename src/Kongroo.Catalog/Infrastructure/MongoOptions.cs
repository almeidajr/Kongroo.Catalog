using System.ComponentModel.DataAnnotations;

namespace Kongroo.Catalog.Infrastructure;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string Database { get; init; } = string.Empty;
}
