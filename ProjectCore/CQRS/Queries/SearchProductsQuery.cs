using Bulky.Models;
using MediatR;
using ProjectCore.Models.AI;

namespace ProjectCore.CQRS.Queries
{
    public record SearchProductsQuery(
        string QueryText,
        int TopK=5,
        bool UserQueryExpansion= false
        ) : IRequest<SearchResult<Product>>;
}
