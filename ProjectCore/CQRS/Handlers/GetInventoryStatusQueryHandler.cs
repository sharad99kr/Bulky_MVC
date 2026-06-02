using Bulky.DataAccess.Repository.IRepository;
using MediatR;
using ProjectCore.CQRS.Queries;

namespace ProjectCore.CQRS.Handlers
{
    public class GetInventoryStatusQueryHandler : IRequestHandler<GetInventoryStatusQuery, InventoryStatusResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int LowStockThreshold = 10; // Example threshold for low stock

        public GetInventoryStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<InventoryStatusResult> Handle(GetInventoryStatusQuery request, CancellationToken cancellationToken)
        {
            var products = _unitOfWork.Product.Get(p=>p.Id==request.ProductId);
            if(products == null)
            {
                return Task.FromResult(new InventoryStatusResult(
                
                     request.ProductId,
                     "Product not found",
                     0,
                     false
                ));
            }

            // TODO Week 5: Product model has no Quantity property.
            // Stock tracking will be added when the Inventory domain is built.
            // Returning placeholder values until then.
            return Task.FromResult(new InventoryStatusResult (
                    products.Id,
                    products.Title,
                     0,
                     false
            ));
        }
    }
}
