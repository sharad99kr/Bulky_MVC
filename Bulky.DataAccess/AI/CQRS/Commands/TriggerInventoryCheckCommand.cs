using Bulky.DataAccess.AI.Inventory.Interfaces;
using MediatR;

namespace Bulky.DataAccess.AI.CQRS.Commands
{
    public record TriggerInventoryCheckCommand() : IRequest<InventoryCheckResult>;

}
