using Bulky.DataAccess.Repository.IRepository;
using MediatR;
using ProjectCore.CQRS.Commands;

namespace ProjectCore.CQRS.Handlers
{
    public class SeedStockCommandHandler : IRequestHandler<SeedStockCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeedStockCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<int> Handle(SeedStockCommand request, CancellationToken cancellationToken)
        {
            // For demonstration, we'll just add a few products with stock levels.
            var products = _unitOfWork.Product.GetAll().ToList();

            var i = 0;
            foreach (var product in products)
            {
                // Every 4th product is low (2 units); the rest are healthy (25).
                product.StockQuantity = (i % 4 == 0 ? 2 : 25);
                _unitOfWork.Product.Update(product);
                i++;
            }
            
            _unitOfWork.Save();
            return Task.FromResult(products.Count);
        }
    }
}
