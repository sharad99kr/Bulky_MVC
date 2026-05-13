using Bulky.DataAccess.Repository.IRepository;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Embeddings;

namespace ProjectCore.Services.AI
{
    public class AzureEmbeddingService : IEmbeddingService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
        private readonly ILogger<AzureEmbeddingService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default) {
            var results = await _embeddings.GenerateAsync(new[] { text }, cancellationToken: ct);
            return results[0].Vector.ToArray();
        }

        public async Task GenerateProductEmbeddingslAsync(IEnumerable<int> productIds, CancellationToken ct = default) {
            if(productIds == null || !productIds.Any()) {
                _logger.LogWarning("No product IDs provided for embedding generation.");
                return;
            }


            foreach(var productId in productIds) {
                try {
                    var product = _unitOfWork.Product.Get(p => p.Id == productId);
                    if(product == null) {
                        _logger.LogWarning("Product with ID {ProductId} not found. Skipping embedding generation.", productId);
                        continue;
                    }

                    //Combine Title, Description, and Category to create a rich text representation for embedding
                    var textToEmbed = $"{product.Title} {product.Description} {product.Category?.Name}";
                    var embedding = await GetEmbeddingAsync(textToEmbed, ct);

                    // Assuming Product entity has an Embedding property of type float[]
                    product.SearchEmbedding = embedding;
                    product.EmbeddingGeneratedAt = DateTime.UtcNow;
                    _unitOfWork.Product.Update(product);
                    _unitOfWork.Save();

                    _logger.LogInformation("Generated embedding for Product ID {ProductId} : {Title}", productId, product.Title);

                } catch(OperationCanceledException) {
                    throw;
                } catch(Exception ex) {
                    _logger.LogError(ex, "Error generating embedding for Product ID {ProductId}", productId);
                }
            }
        }
    }
}
