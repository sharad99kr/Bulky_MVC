using Bulky.DataAccess.AI.Inventory.Models;
using Bulky.DataAccess.Repository.IRepository;
using MediatR;
using ProjectCore.CQRS.Queries;

namespace ProjectCore.CQRS.Handlers
{
    public class GetInventoryStatusQueryHandler : IRequestHandler<GetInventoryStatusQuery, InventoryStatusResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetInventoryStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<InventoryStatusResult> Handle(GetInventoryStatusQuery request, CancellationToken cancellationToken)
        {
            // TODO : Product has no Quantity field.
            // Real stock tracking will come from the inventory agent
            // reading OrderDetails or a dedicated StockQuantity column added via migration.
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

            // TODO : Product model has no Quantity property.
            // Stock tracking will be added when the Inventory domain is built.
            // Returning placeholder values until then.
            return Task.FromResult(new InventoryStatusResult (
                    products.Id,
                    products.Title,
                    products.StockQuantity, 
                    false
            ));
        }
    }
}
