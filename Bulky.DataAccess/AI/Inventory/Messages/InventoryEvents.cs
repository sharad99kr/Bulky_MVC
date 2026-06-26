namespace Bulky.DataAccess.AI.Inventory.Messages
{
    //Published when SQL stock falls at/below the threshold
    public record LowStockDetected(
        int ProductId,
        string ProductName,
        int SqlQuantity,
        int Threshold,
        string AlertPriority
        ); //"Urgent" if SqlQuantity is 0, otherwise "Routine"

    //PUBLISHED once the Excel warehouse source exists and the reconciliation agent can compare SQL vs Excel stock.
    //The agent will publish this event if the discrepancy is above 40% of the SQL quantity.
    public record StockDiscrepancyDetected(
        int ProductId,
        string ProductName,
        int SQLQuantity,
        int ExcelQuantity,
        int DiscrepancyPercent,
        string AlertPriority
        ); //always "Urgent" above 40% discrepancy
}
