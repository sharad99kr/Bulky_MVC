namespace Bulky.DataAccess.AI.Inventory.Models
{
    public record InventoryStatusResult(
        int ProductId,
        string ProductName,
        int StockQuantity,
        bool IsLowStock);

}
