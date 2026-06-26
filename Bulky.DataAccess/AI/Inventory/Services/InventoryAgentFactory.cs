using Bulky.DataAccess.AI.Inventory.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Bulky.DataAccess.AI.Inventory.Services
{
    public class InventoryAgentFactory : IInventoryAgentFactory
    {
        private const string SqlAgentInstructions =
            "You are the Inventory SQL Agent. Call get_low_stock_products to get " +
            "the list of products at or below the stock threshold. Report each " +
            "low product with its title and current quantity as a short list. " +
            "Never invent products. If the list is empty, say inventory is healthy.";

        private const string NotificationAgentInstructions =
            "You are the Notification Agent. Read the SQL agent's findings and " +
            "write a brief, plain-language briefing for a store administrator. " +
            "State how many products are low and name the ones to reorder first. " +
            "Keep it under 120 words. Do not add any product the SQL agent did " +
            "not mention.";

        private readonly IChatClient _chatClient;
        private readonly IInventoryReader _inventoryReader;

        public InventoryAgentFactory(IChatClient chatClient, IInventoryReader inventoryReader) {
            _chatClient = chatClient;
            _inventoryReader = inventoryReader;
        }

        public AIAgent CreateSqlAgent() {
            return new ChatClientAgent(_chatClient,
                name: "InventorySqlAgent",
                instructions: SqlAgentInstructions,
                tools: [AIFunctionFactory.Create(_inventoryReader.GetLowStockProducts)]);
        }

        public AIAgent CreateNotificationAgent() {
            return new ChatClientAgent(
                _chatClient,
                name: "InventoryNotificationAgent",
                instructions: NotificationAgentInstructions);
        }
    }
}
