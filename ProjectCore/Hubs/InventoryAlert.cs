namespace ProjectCore.Hubs
{
    public record InventoryAlert(
        int ProductId,
        string ProductName,
        int Quantity,
        string Priority,
        string Kind,
        string Message,
        DateTime TimestampUtc
        );
}
