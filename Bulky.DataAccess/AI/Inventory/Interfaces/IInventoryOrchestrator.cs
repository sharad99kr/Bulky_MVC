namespace Bulky.DataAccess.AI.Inventory.Interfaces
{
    public interface IInventoryOrchestrator
    {
        Task<InventoryCheckResult> RunInventoryCheckAsync(CancellationToken cancellationToken = default);
    }

    public record InventoryCheckResult(int LowStockCount, int AlertsPublished, string Briefing);
}
