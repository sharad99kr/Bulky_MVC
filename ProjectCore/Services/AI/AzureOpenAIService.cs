using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    //Implements IAIService - the provider layer.
    //Only job: Talk to Azure OpenAI via Semantic Kernel, and return the result to the caller.
    //Knows nothing about books, tones or descriptions
    public class AzureOpenAIService : IAIService
    {
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly Kernel _kernel;
        private readonly IMemoryCache _cache;
        private readonly AISettings _aiSettings;

        public AzureOpenAIService(ILogger<AzureOpenAIService> logger, Kernel kernel, IMemoryCache cache, IOptions<AISettings> aiSettings) {
            _logger = logger;
            _kernel = kernel;
            _cache = cache;
            _aiSettings = aiSettings.Value;
        }

        public async Task<AIResponse<string>> GenerateTextAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken ct = default) {

            var cacheKey = $"ai:text:{systemPrompt.GetHashCode()}:{userPrompt.GetHashCode()}";

            //Check cache first
            if (_aiSettings.EnableCaching && _cache.TryGetValue(cacheKey, out string? cachedResponse)) {
                    _logger.LogInformation($"AI response served from Cache. Key: {cacheKey}");
                    return AIResponse<string>.Ok(cachedResponse!, fromCache: true );
            }
            try {

                //Build chat history for Semantic Kernel and load the prompts
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(userPrompt);

                //Call Semantic Kernel to get AI response (not via HttpClient)
                var chat = _kernel.GetRequiredService<IChatCompletionService>();

                //Set execution parameters
                //Temperature — controls how creative the answer sounds
                //Max tokens — controls how long the answer can be
                var settings = new PromptExecutionSettings();
                settings.ExtensionData = new Dictionary<string, object>();
                settings.ExtensionData["temperature"]=_aiSettings.Temperature;
                settings.ExtensionData["max_tokens"]=_aiSettings.MaxTokens;

                //Get the response from Semantic Kernel with all three parameters: chat history, execution settings and cancellation token
                var result = await chat.GetChatMessageContentAsync(chatHistory, settings, _kernel, ct);
                var text = result.Content ?? string.Empty;

                //cache the result
                if (_aiSettings.EnableCaching) {
                    
                    _cache.Set(
                        cacheKey, 
                        text, 
                        TimeSpan.FromMinutes(
                            _aiSettings.CacheDurationMinutes)
                        );

                    _logger.LogInformation($"AI response cached. Key: " +
                                            $"{cacheKey}");

                }

                _logger.LogInformation(
                    $"AI text generated successfully!" +
                    $"Prompt: {userPrompt}, " +
                    $"Tokens Used: {result.Metadata?["Usage"]?.ToString() ?? 
                                    "unknown"}");


                return AIResponse<string>.Ok(text);

            } catch(Exception ex) {
                _logger.LogError(ex, "AI text generation failed: Prompt: {Prompt}", userPrompt);
                return AIResponse<string>.Fail("AI service is temporarily unavailable");

            }
        }
        public async Task<bool> IsAvailableAsync(CancellationToken ct = default) {
            //"TODO : THis test might return cached result as true even if service is down. Need to implement a better health check mechanism"
            var result = await GenerateTextAsync("You are a test!", "Reply OK", ct);
                return result.Success;
        }
    }
}
