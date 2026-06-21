using ProjectCore.CQRS.Queries;

namespace ProjectCore.Services.AI.Inventory
{
    public interface IInventoryReader
    {
        IReadOnlyList<InventoryStatusResult> GetLowStockProducts();

        InventoryStatusResult? GetProductStock(int productId);
    }
}
