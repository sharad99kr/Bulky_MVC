using MediatR;
using ProjectCore.Services.AI.Inventory;

namespace ProjectCore.CQRS.Commands
{
    public record TriggerInventoryCheckCommand() : IRequest<InventoryCheckResult>;

}
