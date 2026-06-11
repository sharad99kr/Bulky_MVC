
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
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
        private readonly IChatMessageRepository _chatMessageRepository;

        private const int MaxTokenPerTurn = 500;
        private const int MaxContextProducts = 3;
        private const string FallBackMessage =
                    "I'm having trouble connecting to my knowledge base right now. " +
                    "Please try again in a moment, or browse our catalogue directly.";

        public ChatService(IMediator mediator, ILogger<ChatService> logger, IKernelPluginFactory pluginFactory, IChatMessageRepository chatRepository) {
            _mediator = mediator;
            _logger = logger;
            _pluginFactory = pluginFactory;
            _chatMessageRepository = chatRepository;
        }
        public async Task<ChatResponse> SendMessageAsync(string userMessage,
                                                    Guid? conversationId,
                                                    string userId,
                                                    CancellationToken cancellationToken = default) {
            
            var resolvedId = conversationId??Guid.NewGuid();

            try {

                //step 0: load history from DB (replaces client-sent history)
                var storedTurns = await _chatMessageRepository.
                                            GetRecentAsync(resolvedId, userId, count: 6);

                //step 1: RAG context injection
                var ragContext = await BuildRagContextAsync(userMessage, cancellationToken);

                //step 2: build kernel with plugins attached
                var kernel = _pluginFactory.CreateKernelWithPlugins();

                //step 3: build ChatHistory with system prompt + history
                var chatHistory = BuildChatHistory(ragContext, storedTurns);

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

                // Step 6 — Persist both turns atomically
                await _chatMessageRepository.SaveTurnsAsync(
                                        resolvedId,
                                        userId,
                                        userMessage,
                                        responseText,
                                        tokensUsed:MaxTokenPerTurn,
                                        fallbackUsed:false);

                _logger.LogInformation(
                    "[Chat] Successfully processed chat message. " +
                    "Tokens used: {TokensUsed}, " +
                    "User Message Length: {UserMsgLen}, " +
                    "Fallback Used: {FallbackUsed}",
                    MaxTokenPerTurn, userMessage.Length, false);

                return new ChatResponse(
                    Message: responseText,
                    ConversationId: resolvedId,
                    FromCache: false,
                    FallbackUsed: false,
                    TokensUsed: MaxTokenPerTurn,
                    WarningMessage:null);

            } catch(Exception ex) {
                _logger.LogError(ex, "[Chat] Error occurred while processing chat message. SendMessageAsync failed - using fallback");
                try {
                    await _chatMessageRepository.SaveTurnsAsync(
                        resolvedId,
                        userId,
                        userMessage,
                        assistantMessage: FallBackMessage,
                        tokensUsed: 0,
                        fallbackUsed: true);
                } catch(Exception ex2) {
                    /* persistence failure must not mask the original error */
                }

                return new ChatResponse(
                    Message: FallBackMessage,
                    ConversationId: resolvedId,
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
                                        IEnumerable<ChatMessage> storedTurns) {


            //System prompt with RAG context
            var systemPrompt = ChatPromptBuilder.BuildSystemPrompt(ragContext);

            var chatHistory = new ChatHistory(systemPrompt);

            // storedTurns already limited to 6 by GetRecentAsync, already ordered
            // oldest-first after the DESC + reverse in the repository
            foreach(var turn in storedTurns) {
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
