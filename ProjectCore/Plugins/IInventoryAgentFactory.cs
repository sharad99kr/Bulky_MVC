using Microsoft.Agents.AI;
namespace ProjectCore.Plugins
{
    public interface IInventoryAgentFactory
    {
        AIAgent CreateSqlAgent(); //has the get_low_stock_products tool
        AIAgent CreateNotificationAgent(); //phrases the admin briefing
    }
}
