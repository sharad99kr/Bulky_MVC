using Bulky.Models;
using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    public interface ISearchService
    {
        Task<SearchResult<Product>> SemanticSearchAsync(string query, int topK=5, CancellationToken ct=default);

        Task<SearchResult<Product>> HybridSearchAsync(string query, int topK = 5, CancellationToken ct = default);

        //Task<SearchResult<Product>> AzureAISearchAsync(string query, int topK = 5, CancellationToken ct = default);

    }
}
