using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    public class ProductSearchService : ISearchService
    {
        private const float LowConfidenceThreshold = 0.75f;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddings;
        private readonly ILogger<ProductSearchService> _logger;
        public ProductSearchService(IUnitOfWork unitOfWork, IEmbeddingService embeddings, ILogger<ProductSearchService> logger) {
            _unitOfWork = unitOfWork;
            _embeddings = embeddings;
            _logger = logger;
        }

        public async Task<SearchResult<Product>> SemanticSearchAsync(string query, int topK = 5, CancellationToken ct = default) {
            try {
                var queryVector = await _embeddings.GetEmbeddingAsync(query, ct);

                var allProducts = _unitOfWork.Product
                                    .GetAll(includeProperties: "Category,ProductImages")
                                    .Where(p => p.SearchEmbedding != null)
                                    .ToList();
                var scored = allProducts
                                    .Select(p => new {
                                        Product = p,
                                        Score = CosineSimilarity(queryVector, p.SearchEmbedding!)
                                    }).OrderByDescending(x => x.Score)
                                    .Take(topK)
                                    .ToList();

                float topScore = scored.FirstOrDefault()?.Score ?? 0;

                bool lowConfidence = topScore < LowConfidenceThreshold; // Threshold for low confidence, can be adjusted
                if(lowConfidence) {
                    _logger.LogWarning("Low confidence in search results for query: {Query}. Top score: {TopScore}", query, topScore);
                }

                _logger.LogInformation("Semantic search completed for " +
                                            "query: {Query}. " +
                                            "Top score: {TopScore}. " +
                                            "Low confidence: {LowConfidence}",
                                            query, topScore, lowConfidence);

                return new SearchResult<Product> {
                    Items = scored.Select(x => x.Product).ToList(),
                    TopScore = topScore,
                    LowConfidence = lowConfidence
                };
            } catch(Exception ex) {
                _logger.LogError(ex, "Error during semantic search for query: '{Query}'", query);
                throw; // Let the caller handle the fallback to keyword search

            }

        }
        IReadOnlyList<Product> KeywordSearch(string query, int topK = 5) {
            IReadOnlyList<Product> allProducts = _unitOfWork.Product
                                .GetAll(includeProperties: "Category,ProductImages")
                                .Where(p => p.Title.Contains(query) || p.Description.Contains(query))
                                .Take(topK)
                                .ToList();
            return allProducts;
        }

        public async Task<SearchResult<Product>> HybridSearchAsync(string query, int topK = 5, CancellationToken ct = default) {

            IReadOnlyList<Product> keywordResult;
            try {
                 keywordResult = KeywordSearch(query, topK);
            } catch(Exception ex) {
                _logger.LogError(ex, "Keyword search failed for '{Query}' — no fallback available", query);
                throw; // can't fall back to keyword if keyword itself is broken

            }

            try { 
                var semanticTask = SemanticSearchAsync(query, topK, ct);
                var semanticResult = await semanticTask;

                //semantic first, then keyword results, and remove duplicates
                var combined = semanticResult.Items
                                .Union(keywordResult, ProductIdComparer.Instance)
                                .Take(topK)
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
