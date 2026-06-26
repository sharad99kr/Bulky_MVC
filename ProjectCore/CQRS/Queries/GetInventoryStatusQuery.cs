using Bulky.DataAccess.AI.Inventory.Models;
using MediatR;

namespace ProjectCore.CQRS.Queries
{
    public record GetInventoryStatusQuery(int ProductId)
        : IRequest<InventoryStatusResult>;

}
