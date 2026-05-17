using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using ProjectCore.Models.AI;
using System.Linq;

namespace ProjectCore.Services.AI
{
    public class ProductSearchService : ISearchService
    {
        private const float LowConfidenceThreshold = 0.4f;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddings;
        private readonly ILogger<ProductSearchService> _logger;
        private readonly IChatCompletionService _chatCompletionService;
        public ProductSearchService(IUnitOfWork unitOfWork, 
            IEmbeddingService embeddings, 
            ILogger<ProductSearchService> logger, 
            IChatCompletionService chatCompletionService) {

            _unitOfWork = unitOfWork;
            _embeddings = embeddings;
            _logger = logger;
            _chatCompletionService = chatCompletionService;
        }

        public async Task<string> ExpandQueryAsync(string query, CancellationToken ct) {
            var prompt = $"""
                        Expand this book search query into a short descriptive phrase 
                        that includes genre, mood, and themes. 2-3 sentences max.
                        Query: {query}
                        """;
            // call IChatCompletionService, return the expanded text
            try {
                var history = new ChatHistory();
                history.AddUserMessage(prompt);

                var response = await _chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: ct);

                

                _logger.LogInformation("ExpandQuery returned of user {Query}: '{Response}'",query, response.Content);
                return response.Content; // Return negative score if parsing fails

            } catch(Exception ex) {
                _logger.LogError(ex, "Error expanding user query");
                return ""; // Return negative score in case of error
            }
        }

        public async Task<SearchResult<Product>> SemanticSearchAsync(string query,
                                                                        int topK = 5,
                                                                        bool useQueryExpansion = false,
                                                                        CancellationToken ct = default) {
            try {

                var vectorInput = query;
                if(useQueryExpansion) {
                    var expanded = await ExpandQueryAsync(query, ct);
                    vectorInput = string.IsNullOrWhiteSpace(expanded) ? query : expanded;
                }

                var queryVector = await _embeddings.GetEmbeddingAsync(vectorInput, ct);
                

                var allProducts = _unitOfWork.Product
                                    .GetAll(includeProperties: "Category,ProductImages")
                                    .Where(p => p.SearchEmbeddingData != null)
                                    .ToList();
                
              
                var scored = allProducts
                            .Select(p => new {
                                Product = p,
                                Score = CosineSimilarity(queryVector, p.SearchEmbedding!)
                            }).OrderByDescending(x => x.Score)
                            .ToList(); // don't Take(topK) yet. We need to iterate through all results to determine confidence, which may require looking beyond just the top K if there are a lot of similarly scored items at the top.

                float topScore = scored.ElementAtOrDefault(0)?.Score ?? 0;
                float secondScore = scored.ElementAtOrDefault(1)?.Score ?? 0;
                var scoreGap = topScore - secondScore;
                bool lowConfidence = false;
                if(topScore < LowConfidenceThreshold ||           // the best match is just too weak regardless of anything else
                    (topScore < 0.50f && scoreGap < 0.10f) ||     //mediocre score AND the top two results are very close together, so even the best match isn't convincingly better
                    (topScore < 0.60f && scoreGap < 0.05f)) {     //decent score but the top two results are almost identical in score, so the ranking is basically a coin flip

                    lowConfidence = true;// Threshold for low confidence, can be adjusted
                }


                if(lowConfidence) {
                    _logger.LogWarning("Low confidence in search results for query: {Query}. Top score: {TopScore}", query, topScore);
                }

                _logger.LogInformation("Semantic search completed for " +
                                            "query: {Query}. " +
                                            "Top score: {TopScore}. " +
                                            "Low confidence: {LowConfidence}",
                                            query, topScore, lowConfidence);

                return new SearchResult<Product> {
                    Items = scored.Take(topK).Select(x => x.Product).ToList(),
                    TopScore = topScore,
                    LowConfidence = lowConfidence
                };
            } catch(Exception ex) {
                _logger.LogError(ex, "Error during semantic search for query: '{Query}'", query);
                throw; // Let the caller handle the fallback to keyword search

            }

        }
        IReadOnlyList<Product> KeywordSearch(string query, int topK = 5) {
            var queryWords = ExtractWordsFromPhrase(query);
            IReadOnlyList<Product> allProducts = _unitOfWork.Product
                                .GetAll(includeProperties: "Category,ProductImages")
                                .Where(p => queryWords.Any(q => p.Title.Contains(q) || p.Description.Contains(q)))
                                .Take(topK)
                                .ToList();
            return allProducts;
        }

        List<string> ExtractWordsFromPhrase(string query) {
            if(string.IsNullOrWhiteSpace(query))
                return new List<string>();

            // Split by spaces, remove punctuation, and filter out short words
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); 
            return words.ToList();
        }

        public async Task<SearchResult<Product>> HybridSearchAsync(string query,
                                                                        int topK = 5,
                                                                        bool useQueryExpansion = false,
                                                                        CancellationToken ct = default) {

            IReadOnlyList<Product> keywordResult;
            try {
                 keywordResult = KeywordSearch(query, topK);
            } catch(Exception ex) {
                _logger.LogError(ex, "Keyword search failed for '{Query}' — no fallback available", query);
                throw; // can't fall back to keyword if keyword itself is broken

            }

            try { 
                var semanticTask = SemanticSearchAsync(query, topK, useQueryExpansion,ct);
                var semanticResult = await semanticTask;

                //semantic first, then keyword results, and remove duplicates
                var combined = semanticResult.Items
                                .Union(keywordResult, ProductIdComparer.Instance)
                                .Take(topK) //take topK after combining and deduping, so we ensure we return up to topK results total, not topK from each method which could result in more than topK total
                                .ToList();

                //Confidence is based on the semantic search score, if the top score is below the threshold, we consider it low confidence -
                //keyword results being added doesn't increase confidence, it's just a fallback to ensure we return some results
                return new SearchResult<Product> {
                    Items = combined,
                    TopScore = semanticResult.TopScore,
                    LowConfidence = semanticResult.LowConfidence
                };
            } catch(Exception ex) {
                _logger.LogWarning(ex, "Error during semantic search for query: '{Query}', falling back to keyword search", query);
                
                return new SearchResult<Product> {
                    Items = keywordResult,
                    TopScore = 0f, // No semantic score available
                    LowConfidence = true // AI unavailable = unknown confidence
                };
            }
        }
        //public Task<SearchResult<Product>> AzureAISearchAsync(string query, int topK = 5, CancellationToken ct = default)
        //{
        //    return _productAIService.AzureAISearchAsync(query, topK, ct);
        //}

        float CosineSimilarity(float[] vectorA, float[] vectorB) {
            if(vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must be of the same length");
            
            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;
            
            for(int i = 0; i < vectorA.Length; i++) {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }
            
            magnitudeA = (float)Math.Sqrt(magnitudeA);
            magnitudeB = (float)Math.Sqrt(magnitudeB);
            if(magnitudeA == 0 || magnitudeB == 0)
                return 0;
            return dotProduct / (magnitudeA * magnitudeB);
        }
    }

    public class ProductIdComparer : IEqualityComparer<Product>
    {
        public readonly static ProductIdComparer Instance = new ProductIdComparer();
        public bool Equals(Product? x, Product? y) {
            return (x?.Id == y?.Id);
             
        }
        public int GetHashCode(Product? obj) {
            //GetHashCode is required by IEqualityComparer<T> and it's what makes equality checks efficient.
            //Union uses a hash set internally:
            //it first compares hash codes, and only calls Equals if the hash codes match.
            return obj?.Id.GetHashCode() ?? 0;
        }

    }

}
