
using MediatR;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ProjectCore.CQRS.Queries;
using ProjectCore.Plugins;
using System.Text;

namespace ProjectCore.Services.AI
{
    public class ChatService : IChatService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChatService> _logger;
        private readonly IKernelPluginFactory _pluginFactory;

        private const int MaxTokenPerTurn = 500;
        private const int MaxContextProducts = 3;
        private const string FallBackMessage =
                    "I'm having trouble connecting to my knowledge base right now. " +
                    "Please try again in a moment, or browse our catalogue directly.";

        public ChatService(IMediator mediator, ILogger<ChatService> logger, IKernelPluginFactory pluginFactory) {
            _mediator = mediator;
            _logger = logger;
            _pluginFactory = pluginFactory;
        }
        public async Task<ChatResponse> SendMessageAsync(string userMessage,
                                                    IEnumerable<ChatTurn> history,
                                                    CancellationToken cancellationToken = default) {
            try {
                //step 1: RAG context injection
                var ragContext = await BuildRagContextAsync(userMessage, cancellationToken);

                //step 2: build kernel with plugins attached
                var kernel = _pluginFactory.CreateKernelWithPlugins();

                //step 3: build ChatHistory with system prompt + history
                var chatHistory = BuildChatHistory(ragContext, history);

                //step 4: Add user message as last turn
                chatHistory.AddUserMessage(userMessage);

                //step 5: Invoke with token budget + auto invoke plugins
                var settings = new OpenAIPromptExecutionSettings {
                    MaxTokens = MaxTokenPerTurn,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                var chatSvc = kernel.GetRequiredService<IChatCompletionService>();
                var result = await chatSvc.GetChatMessageContentAsync(
                    chatHistory,
                    settings,
                    kernel,
                    cancellationToken);

                var responseText = result.Content ?? FallBackMessage;

                _logger.LogInformation(
                    "[Chat] Successfully processed chat message. " +
                    "Tokens used: {TokensUsed}, " +
                    "User Message Length: {UserMsgLen}, " +
                    "Fallback Used: {FallbackUsed}",
                    MaxTokenPerTurn, userMessage.Length, false);

                return new ChatResponse(
                    Message: responseText,
                    FromCache: false,
                    FallbackUsed: false,
                    TokensUsed: MaxTokenPerTurn);

            } catch(Exception ex) {
                _logger.LogError(ex, "[Chat] Error occurred while processing chat message. SendMessageAsync failed - using fallback");
                return new ChatResponse(
                    Message: FallBackMessage,
                    FromCache: false,
                    FallbackUsed: true,
                    TokensUsed: 0);

            }
        }

        private async Task<string> BuildRagContextAsync(string userMessage, CancellationToken cancellationToken) {
            try {
                var searchResults = await _mediator.Send(
                                            new SearchProductsQuery(
                                                userMessage,
                                                TopK: MaxContextProducts,
                                                UserQueryExpansion: false),
                                            cancellationToken);
                if(!searchResults.Items.Any()) {
                    return string.Empty;
                }

                var sb = new StringBuilder();
                sb.AppendLine("PRODUCT CATALOGUE CONTEXT:");

                foreach(var item in searchResults.Items) {
                    sb.AppendLine(
                        $"- {item.Title} by {item.Author} " +
                        $"| Category: {item.Category?.Name ?? "Unknown"} " +
                        $"| Price: {item.Price:C} " +
                        $"| Description: {item.Description?[..Math.Min(150, item.Description.Length)]}...");
                }

                if(searchResults.LowConfidence) {
                    sb.AppendLine("NOTE: The relevance of the above results to your query is uncertain.");
                }
                return sb.ToString();
            } catch(Exception ex) {
                _logger.LogError(ex, "[Chat] Error occurred while RAG context build. Continuing without RAG context.");
                return string.Empty; //non-fatal - we can still proceed with the chat without RAG context
            }
        }

        private ChatHistory BuildChatHistory(string ragContext,
                                        IEnumerable<ChatTurn> history) {


            //System prompt with RAG context
            var systemPrompt = ChatPromptBuilder.BuildSystemPrompt(ragContext);

            var chatHistory = new ChatHistory(systemPrompt);

            //Replay prior turns - max last 6 turns to manage token cost and relevance
            foreach(var turn in history.TakeLast(6)) {
                if(turn.Role == "User") {
                    chatHistory.AddUserMessage(turn.Content);
                } else if(turn.Role == "assistent") {
                    chatHistory.AddAssistantMessage(turn.Content);
                }
            }
            return chatHistory;
        }
    }
        
}
