using MediatR;
using ProjectCore.CQRS.Commands;
using ProjectCore.Services.AI.Inventory;

namespace ProjectCore.CQRS.Handlers
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
