using Bulky.DataAccess.AI.Inventory.Models;

namespace Bulky.DataAccess.AI.Inventory.Interfaces
{
    public interface IInventoryReader
    {
        IReadOnlyList<InventoryStatusResult> GetLowStockProducts();

        InventoryStatusResult? GetProductStock(int productId);
    }
}
