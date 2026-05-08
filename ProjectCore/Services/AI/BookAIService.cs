using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    
    public class BookAIService : IProductAIService
    {

        private readonly ILogger<BookAIService> _logger;
        private readonly IAIService _aiService;

        public BookAIService(ILogger<BookAIService> logger, IAIService aiService) {
            _logger = logger;
            _aiService = aiService;
        }

        public async Task<AIResponse<AIProductDescriptionResult>> GenerateDescriptionAsync(
                AIProductDescriptionRequest request,
                CancellationToken ct = default) {
            
            var systemPrompt = BuildSystemPrompt(request.Tone);
            var userPrompt = BuildUserPrompt(request);

            var textResult = await _aiService.GenerateTextAsync(systemPrompt, userPrompt, ct);
            if (!textResult.Success) {
                _logger.LogError("AI generation failed: {ErrorMessage}", textResult.ErrorMessage);
                return AIResponse<AIProductDescriptionResult>.Fail(textResult.ErrorMessage ?? "Unknown error");
            }

            var productDescription = new AIProductDescriptionResult {
                                    Description = textResult.Data ?? "",
                                    Tone = request.Tone.ToString(),
                                    GeneratedAt = DateTime.UtcNow};

            return AIResponse<AIProductDescriptionResult>.Ok(
                                    data: productDescription, 
                                    tokens : textResult.TokensUsed,
                                    fromCache: textResult.FromCache);

        }

        //Returns all tones the generator supports( for the UI dropdown)
        public IEnumerable<string> GetAvailableTones() {
            return Enum.GetNames<DescriptionTone>();
        }

        private static string BuildUserPrompt(AIProductDescriptionRequest req) {
            var author = req.Author is not null ? $" by {req.Author}" : "";
            return $"Write a {req.MaxSentences}-sentence product description for " +
                   $"'{req.ProductName}'{author} in the {req.Category} category.";
        }
        private static string BuildSystemPrompt(DescriptionTone tone) {
            switch(tone) {
                case DescriptionTone.Professional:
                        return "You are a professional copywriter for a premium book retailer. " +
                               "Write concise, authoritative product descriptions that highlight literary merit. " +
                               "Never fabricate awards, authors, or facts not provided.";
                case DescriptionTone.Casual:
                        return "You are a friendly book recommender writing for a general audience. " +
                                "Keep descriptions warm, approachable, and enthusiastic. 2–3 sentences max.";
                case DescriptionTone.Playful:
                        return "You write fun, engaging book blurbs for a young adult audience. " +
                                "Use accessible language, avoid jargon. Make it sound exciting.";
                case DescriptionTone.Academic:
                        return "You write scholarly book descriptions for an academic catalogue. " +
                                "Emphasise themes, critical reception, and academic relevance.";
                default:
                    return "Write a clear, professional product description.";
            }
        }
    }
}
