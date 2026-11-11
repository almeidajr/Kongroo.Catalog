namespace Kongroo.Catalog.Infrastructure;

/// <summary>
/// SNS topic names used with the Amazon SQS transport. The cross-service ones are shared by
/// contract with Kongroo.Payments and the Kongroo.Notifications SAM template — change all three together.
/// </summary>
public static class MessagingTopics
{
    public const string OrderPlaced = "kongroo-order-placed";
    public const string PaymentProcessed = "kongroo-payment-processed";

    /// <summary>Catalog-internal: Catalog publishes and consumes it to keep Game Search in step.</summary>
    public const string GameIndexRequested = "kongroo-game-index-requested";
}
