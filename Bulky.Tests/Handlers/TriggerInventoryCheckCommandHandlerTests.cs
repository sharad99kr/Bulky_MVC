using Bulky.DataAccess.AI.CQRS.Commands;
using Bulky.DataAccess.AI.CQRS.Handlers;
using Bulky.DataAccess.AI.Inventory.Interfaces;
using Moq;
using ProjectCore.CQRS.Commands;
using ProjectCore.CQRS.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests.Handlers
{
    public class TriggerInventoryCheckCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesToOrchestrator() {
            var orchestrator = new Mock<IInventoryOrchestrator>();
            orchestrator.Setup( o => o.RunInventoryCheckAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InventoryCheckResult(3,3,"3 products low."));

            var handler = new TriggerInventoryCheckCommandHandler(orchestrator.Object);

            var result = await handler.Handle(new TriggerInventoryCheckCommand(), CancellationToken.None);

            Assert.Equal(3, result.LowStockCount);
            Assert.Equal(3, result.AlertsPublished);

            orchestrator.Verify(
                o => o.RunInventoryCheckAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
