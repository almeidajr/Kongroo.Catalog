using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class ApplyPaymentResultCommandHandler(CatalogDbContext context)
{
    public async Task HandleAsync(ApplyPaymentResultCommand command, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(command.OrderId);

        var order =
            await context.Orders.SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), $"identifier '{command.OrderId}'");

        if (order.Status != OrderStatus.Pending)
        {
            return; // already decided — redelivered PaymentProcessed event
        }

        if (command.IsApproved)
        {
            order.MarkPaid(command.ProcessedAt);

            var ownerships = order.Lines.Select(line =>
                Ownership.AcquireFromOrder(order.CustomerId, line.GameId, order.Id, command.ProcessedAt)
            );
            context.Ownerships.AddRange(ownerships);
        }
        else
        {
            order.Reject();
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
