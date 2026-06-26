using Microsoft.Agents.AI;
namespace Bulky.DataAccess.AI.Inventory.Interfaces
{
    public interface IInventoryAgentFactory
    {
        AIAgent CreateSqlAgent(); //has the get_low_stock_products tool
        AIAgent CreateNotificationAgent(); //phrases the admin briefing
    }
}
