using Bulky.Models;
using MediatR;
using ProjectCore.CQRS.Queries;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;

namespace ProjectCore.CQRS.Handlers
{
    public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, SearchResult<Product>>
    {
        private readonly ISearchService _SearchService;
        public SearchProductsQueryHandler(ISearchService productSearchService)
        {
            _SearchService = productSearchService;
        }
        public Task<SearchResult<Product>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
        {
            // Call the search service to perform a product search
            var results = _SearchService.HybridSearchAsync(request.QueryText, request.TopK,request.UserQueryExpansion, cancellationToken);
            return results;
        }
}
