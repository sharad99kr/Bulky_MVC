using Bulky.DataAccess.AI.CQRS.Commands;
using Bulky.DataAccess.AI.Inventory.Interfaces;
using MediatR;

namespace Bulky.DataAccess.AI.CQRS.Handlers
{
    public class TriggerInventoryCheckCommandHandler
                                    : IRequestHandler<TriggerInventoryCheckCommand, InventoryCheckResult>
    {
        private readonly IInventoryOrchestrator _orchestrator;

        public TriggerInventoryCheckCommandHandler(IInventoryOrchestrator orchestrator)
            => _orchestrator = orchestrator;

        public Task<InventoryCheckResult> Handle(
            TriggerInventoryCheckCommand request, CancellationToken ct)
            => _orchestrator.RunInventoryCheckAsync(ct);
    }
}
