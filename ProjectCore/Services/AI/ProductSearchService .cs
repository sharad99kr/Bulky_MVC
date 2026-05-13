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
        }
        //public Task<SearchResult<Product>> HybridSearchAsync(string query, int topK = 5, CancellationToken ct = default) {
        //    return _productAIService.HybridSearchAsync(query, topK, ct);
        //}
        //public Task<SearchResult<Product>> AzureAISearchAsync(string query, int topK = 5, CancellationToken ct = default)
        //{
        //    return _productAIService.AzureAISearchAsync(query, topK, ct);
        //}

        public float CosineSimilarity(float[] vectorA, float[] vectorB) {
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
}
