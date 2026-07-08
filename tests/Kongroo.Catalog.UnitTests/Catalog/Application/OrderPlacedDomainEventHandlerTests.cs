using Kongroo.Catalog.Application;
using Kongroo.Catalog.Contracts;
using Kongroo.Catalog.Domain;
using MassTransit;
using NSubstitute;

namespace Kongroo.Catalog.UnitTests.Catalog.Application;

public sealed class OrderPlacedDomainEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithOrderPlacedDomainEvent_ShouldPublishIntegrationEventWithUserAndGamePriceDetails()
    {
        // Arrange
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var handler = new OrderPlacedDomainEventHandler(publishEndpoint);

        var orderId = OrderId.Create();
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var gameId = GameId.From(Guid.CreateVersion7());
        var secondGameId = GameId.From(Guid.CreateVersion7());
        var gamePrice = Money.From(17.25m, Currency.Usd);
        var secondGamePrice = Money.From(25.25m, Currency.Usd);
        var total = Money.From(42.50m, Currency.Usd);
        var domainEvent = new OrderPlacedDomainEvent(
            orderId,
            customerId,
            "ada@example.com",
            "Ada Lovelace",
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            total,
            [new OrderPlacedGameLine(gameId, gamePrice), new OrderPlacedGameLine(secondGameId, secondGamePrice)]
        );

        // Act
        await handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        // Assert
        await publishEndpoint
            .Received(1)
            .Publish(
                Arg.Is<OrderPlacedIntegrationEvent>(integrationEvent =>
                    integrationEvent.OrderId == orderId.Value
                    && integrationEvent.CustomerId == customerId.Value
                    && integrationEvent.CustomerEmail == "ada@example.com"
                    && integrationEvent.CustomerName == "Ada Lovelace"
                    && integrationEvent.TotalAmount == 42.50m
                    && integrationEvent.Currency == "USD"
                    && integrationEvent.Lines.Count == 2
                    && integrationEvent.Lines[0].GameId == gameId.Value
                    && integrationEvent.Lines[0].UnitPrice == 17.25m
                    && integrationEvent.Lines[1].GameId == secondGameId.Value
                    && integrationEvent.Lines[1].UnitPrice == 25.25m
                ),
                TestContext.Current.CancellationToken
            );
    }
}
