using Kongroo.BuildingBlocks.Contracts;
using MassTransit;

namespace Kongroo.Catalog.Application;

/// <summary>Applies the payment decision to the originating order when Payments reports a result.</summary>
public sealed class PaymentProcessedIntegrationEventConsumer(ApplyPaymentResultCommandHandler handler)
    : IConsumer<PaymentProcessedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentProcessedIntegrationEvent> context)
    {
        var message = context.Message;

        return handler.HandleAsync(
            new ApplyPaymentResultCommand(message.OrderId, message.Approved, message.ProcessedAt),
            context.CancellationToken
        );
    }
}
