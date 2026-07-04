using Kongroo.Catalog.Application;
using Kongroo.Catalog.Contracts;
using Kongroo.Catalog.Domain;
using MassTransit;
using NSubstitute;

namespace Kongroo.Catalog.UnitTests.Catalog.Application;

public sealed class OrderPlacedDomainEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithOrderPlacedDomainEvent_ShouldPublishIntegrationEventWithMappedFields()
    {
        // Arrange
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var handler = new OrderPlacedDomainEventHandler(publishEndpoint);

        var orderId = OrderId.Create();
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var total = Money.From(42.50m, Currency.Usd);
        var domainEvent = new OrderPlacedDomainEvent(
            orderId,
            customerId,
            "ada@example.com",
            "Ada Lovelace",
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            total,
            [GameId.From(Guid.CreateVersion7())]
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
                    && integrationEvent.Email == "ada@example.com"
                    && integrationEvent.CustomerName == "Ada Lovelace"
                    && integrationEvent.Amount == 42.50m
                    && integrationEvent.Currency == "USD"
                ),
                TestContext.Current.CancellationToken
            );
    }
}
