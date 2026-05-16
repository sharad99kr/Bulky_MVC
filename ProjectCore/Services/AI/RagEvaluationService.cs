using Microsoft.SemanticKernel.ChatCompletion;

namespace ProjectCore.Services.AI
{
    public class RagEvaluationService: IRagEvaluationService
    {
        private readonly ILogger<RagEvaluationService> _logger;
        private readonly IChatCompletionService _chatCompletionService;
        public RagEvaluationService(ILogger<RagEvaluationService> logger, IChatCompletionService chatCompletionService) { 
            _logger = logger;
            _chatCompletionService = chatCompletionService;
        }

        public async Task<int> ScoreFaithfulnessAsync(string query, IEnumerable<string> retrievedContext, CancellationToken ct = default) {
            var contextString = string.Join("\n---\n", retrievedContext);
            var judgementPrompt =
                               $"""
                                You are an expert RAG evaluation judge.

                                USER QUERY:
                                {query}

                                RETRIEVED CONTEXT:
                                {contextString}

                                Score how well the retrieved context can answer the user query.
                                Use only this scale — reply with the number only, nothing else:

                                1 = Context is completely irrelevant to the query
                                2 = Context is mostly irrelevant with minor overlap
                                3 = Context partially addresses the query
                                4 = Context largely addresses the query with minor gaps
                                5 = Context fully and faithfully addresses the query
                                """;

            try {
                var history = new ChatHistory();
                history.AddUserMessage(judgementPrompt);

                var response = await _chatCompletionService.GetChatMessageContentAsync(history, cancellationToken:ct);

                if(int.TryParse(response.Content?.Trim(), out int score)
                    && score >=1 && score <=5) {

                    _logger.LogInformation("Faithfulness score for query '{Query}': {Score}", query, score);
                    return score; 

                }

                _logger.LogWarning("RagEvaluationService returned unparseable score: '{Response}'", response.Content);
                return -1; // Return negative score if parsing fails

            } catch(Exception ex) {
                _logger.LogError(ex, "Error scoring faithfulness of retrieved context.");
                return -1; // Return negative score in case of error
            }
        }
    }
}
