using MassTransit;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;
using Bulky.DataAccess.AI.Inventory.Interfaces;
using Microsoft.Extensions.Logging;
using Bulky.DataAccess.AI.Inventory.Messages;

namespace Bulky.DataAccess.AI.Inventory.Services
{
    public class InventoryOrchestrationService : IInventoryOrchestrator
    {
        private readonly IInventoryReader _inventoryReader;
        private readonly IInventoryAgentFactory _agentFactory;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<InventoryOrchestrationService> _logger;

        public InventoryOrchestrationService(
            IInventoryReader inventoryReader,
            IInventoryAgentFactory agentFactory,
            IPublishEndpoint publishEndpoint,
            ILogger<InventoryOrchestrationService> logger)
        {
            _inventoryReader = inventoryReader;
            _agentFactory = agentFactory;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<InventoryCheckResult> RunInventoryCheckAsync(CancellationToken cancellationToken = default)
        {
            //Step 1: Deterministic trigger(business critical, testable)
            //A non deterministic LLM must never decide whether a business alert fires.
            //The trigger condition is plain code; the agents only explain the reasoning
            //behind the trigger and provide additional context.

            var lowStockItems =  _inventoryReader.GetLowStockProducts();
            var publishedAlerts = 0;

            foreach(var item in lowStockItems) { 
                
                var priority = item.StockQuantity == 0 ? "Urgent" : "Routine";

                await _publishEndpoint.Publish(new LowStockDetected
                    (   
                    item.ProductId,
                    item.ProductName,
                    item.StockQuantity,
                    InventoryReader.LowStockThreshold,
                    priority)
                , cancellationToken);

                publishedAlerts++;
            }

            _logger.LogInformation("[Inventory] Deterministic scan published {Count} LowStockDetected event(s)", publishedAlerts);

            string briefing;
            try {
                //Step 2: Non-deterministic reasoning: Agent workflow produces a human readable briefing
                briefing = await BuildBriefingAsync(lowStockItems.Count, cancellationToken);

            } catch(Exception ex) {
                _logger.LogWarning(ex, "[Inventory] Outer guard — briefing failed, using summary");
                briefing = DeterministicSummary(lowStockItems.Count);
            }


            return new InventoryCheckResult
            (
                lowStockItems.Count,
                publishedAlerts,
                briefing
             );
                
            
        }

        //The multi-agent layer workflow. Sequential workflow: the SQL agent calls its
        //tool and reports findings; then the notification agent reads the SQL agent's
        //output and produces a briefing.
        private async Task<string> BuildBriefingAsync(int lowStockCount, CancellationToken ct) {

            try {

                AIAgent sqlAgent = _agentFactory.CreateSqlAgent();
                AIAgent notificationAgent = _agentFactory.CreateNotificationAgent();

                //SequentialOrchestration : sqlAgent -> notificationAgent
                Workflow workflow = AgentWorkflowBuilder.BuildSequential("inventory-pipeline",[sqlAgent, notificationAgent]);

                List<ChatMessage> input = [
                        new (ChatRole.User,
                        "Run the scheduled inventory check and brief the administrator.")
                    ];

                await using StreamingRun run =
                    await InProcessExecution.RunStreamingAsync(workflow, input, cancellationToken: ct);

                await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

                string briefing = string.Empty;

                await foreach( WorkflowEvent evt in run.WatchStreamAsync()) {

                    if(evt is WorkflowOutputEvent outputEvent) {
                        if(outputEvent.As<List<ChatMessage>>() is { Count: > 0 } msgs) {
                            briefing = msgs.Last().Text ?? string.Empty;
                        }
                        break;
                    }

                }

                return string.IsNullOrWhiteSpace(briefing)
                    ? DeterministicSummary(lowStockCount)
                    : briefing.Trim();
            
            } catch(Exception ex) { 
                //The briefing is a nice-to-have. If MAF or the model fails,
                //the alert pipeline (Step 1) always run 
                _logger.LogWarning(ex, "[Inventory] Agent briefing failed — using deterministic summary");
                return DeterministicSummary(lowStockCount);

            }
        }

        private static string DeterministicSummary(int lowStockCount) {
            return lowStockCount == 0 ?
                " Inventory is healthy — no products are below the threshold." :
                $"{lowStockCount} product(s) are at or below the stock threshold and need reordering.";
        }

    }
}
