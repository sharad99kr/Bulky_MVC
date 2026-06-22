using MassTransit;
using Microsoft.AspNetCore.SignalR;
using ProjectCore.Hubs;
using ProjectCore.Messages;

namespace ProjectCore.Consumers
{
    public class NotificationConsumer : IConsumer<LowStockDetected>
    {
        private readonly ILogger _logger;
        private readonly IHubContext<InventoryAlertHub> _hub;

        public NotificationConsumer(IHubContext<InventoryAlertHub> hub, ILogger logger) {
            _hub = hub;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<LowStockDetected> context) {
            var m = context.Message;

            var alert = new InventoryAlert(
                ProductId: m.ProductId,
                ProductName: m.ProductName,
                Quantity: m.SqlQuantity,
                Priority: m.AlertPriority,
                Kind: "LowStock",
                Message: $"{m.ProductName} is low — {m.SqlQuantity} left " +
                          $"(threshold {m.Threshold}).",
                TimestampUtc: DateTime.UtcNow
                );

            _logger.LogInformation(
                "[Consumer] LowStockDetected -> SignalR — Product {Id}, Qty {Qty}, {Priority}",
                m.ProductId, m.SqlQuantity, m.AlertPriority);

            await _hub.Clients.All.SendAsync("ReceiveAlert", alert, context.CancellationToken);
        }
    }
}
