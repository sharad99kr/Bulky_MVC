using Microsoft.SemanticKernel;
using ProjectCore.Services.AI;
using System.ComponentModel;

namespace ProjectCore.Plugins
{
    [Description("Searches the product catalogue by natural language query")]
    public class ProductPlugin
    {
        private readonly ISearchService _searchService;

        public ProductPlugin(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [KernelFunction("search_products")]
        [Description("Returns products matching a natural language search query")]
        public async Task<string> SearchProducts(string query, int topK = 3) {
            topK = Math.Min(topK, 10);
            var results = await _searchService.HybridSearchAsync(query, topK, useQueryExpansion:false);
            if(!results.Items.Any())
            {
                return $"No products found matching the query{query}";

            } else {
                return string.Join("\n", results.Items.Select(p =>
                        $"{p.Title} by {p.Author} — {p.Price100:C} — " +
                        $"Category: {p.Category?.Name ?? "Unknown"}"));
            }
        }
    }
}
