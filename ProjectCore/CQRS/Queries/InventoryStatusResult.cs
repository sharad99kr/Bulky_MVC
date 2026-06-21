namespace ProjectCore.CQRS.Queries
{
    public record InventoryStatusResult(
        int ProductId,
        string ProductName,
        int StockQuantity,
        bool IsLowStock);

}
