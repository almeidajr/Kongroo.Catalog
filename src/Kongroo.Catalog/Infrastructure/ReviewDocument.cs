using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>A customer's review of a game, stored in the MongoDB <c>reviews</c> collection.</summary>
/// <remarks>
/// A mutable class with <c>init</c> setters rather than a record because the MongoDB driver's
/// attribute-based class map targets it.
/// </remarks>
public sealed class ReviewDocument
{
    public const string CollectionName = "reviews";

    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; init; }

    [BsonElement("gameId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid GameId { get; init; }

    [BsonElement("customerId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CustomerId { get; init; }

    [BsonElement("customerName")]
    public string CustomerName { get; init; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; init; }

    [BsonElement("text")]
    [BsonIgnoreIfNull]
    public string? Text { get; init; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }
}
