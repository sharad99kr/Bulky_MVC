using Bulky.DataAccess.Repository.IRepository;
using ProjectCore.CQRS.Queries;
using System.ComponentModel;

namespace ProjectCore.Services.AI.Inventory
{
    public class InventoryReader : IInventoryReader
    {
        public const int LowStockThreshold = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryReader> _logger;

        public InventoryReader(IUnitOfWork unitOfWork, ILogger<InventoryReader> logger) {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [Description("Returns every product at or below the low-stock threshold, " +
                "each with its current stock quantity.")]
        public IReadOnlyList<InventoryStatusResult> GetLowStockProducts() {
            var lowStockProducts = _unitOfWork.Product
                .GetAll(p => p.StockQuantity <= LowStockThreshold)
                .Select(p => new InventoryStatusResult(
                    p.Id,
                    p.Title,
                    p.StockQuantity,
                    IsLowStock: true))
                .ToList();
            _logger.LogInformation("[Inventory] Low-stock scan — {Count} product(s) at or below {Threshold}",
            lowStockProducts.Count, LowStockThreshold);
            return lowStockProducts;
        }

        [Description("Returns the current stock quantity for a single product " +
                "by its numeric ID.")]
        public InventoryStatusResult? GetProductStock(int productId) {
            var product = _unitOfWork.Product.Get(p => p.Id == productId);
            if(product == null) {
                _logger.LogWarning("[Inventory] Product ID {ProductId} not found when checking stock.", productId);
                return null;
            }
            return new InventoryStatusResult(
                product.Id,
                product.Title,
                product.StockQuantity,
                IsLowStock: product.StockQuantity <= LowStockThreshold);

        }
    }
}
