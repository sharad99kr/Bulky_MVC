using Bulky.DataAccess.AI.Inventory.Messages;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProjectCore.Consumers;
using ProjectCore.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests.Consumers
{
    public class NotificationConsumerTests
    {
        [Fact]
        public async Task Consumes_LowStockDetected_And_PushesToHub() {
            // A hub context whose Clients.All is a verifiable proxy.
            var clientProxy = new Mock<IClientProxy>();
            var clients = new Mock<IHubClients>();
            clients.Setup(c => c.All).Returns(clientProxy.Object);
            var hub = new Mock<IHubContext<InventoryAlertHub>>();
            hub.Setup(h => h.Clients).Returns(clients.Object);

            await using var provider = new ServiceCollection()
                .AddSingleton(hub.Object)
                .AddLogging()
                .AddMassTransitTestHarness(x => x.AddConsumer<NotificationConsumer>())
                .BuildServiceProvider(true);

            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            try {
                await harness.Bus.Publish(
                    new LowStockDetected(1, "Book A", 0, 5, "Urgent"));

                // The consumer received the message...
                Assert.True(await harness.Consumed.Any<LowStockDetected>());

                // ...and pushed exactly one "ReceiveAlert" to all clients.
                // SignalR's SendAsync is an extension over SendCoreAsync.
                clientProxy.Verify(p => p.SendCoreAsync(
                    "ReceiveAlert",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()), Times.Once);
            } finally {
                await harness.Stop();
            }
        }
    }
}
