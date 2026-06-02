namespace ProjectCore.CQRS.Queries
{
    public record InventoryStatusResult(
        int ProductId,
        string ProductName,
        int Quantity,
        bool IsLowStock
        );
    
}
