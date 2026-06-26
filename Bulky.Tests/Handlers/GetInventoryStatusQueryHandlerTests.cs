using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Moq;
using ProjectCore.CQRS.Handlers;
using ProjectCore.CQRS.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests.Handlers
{
    public class GetInventoryStatusQueryHandlerTests
    {
        private static IUnitOfWork UnitOfWorkReturning(Product product) {
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.Product.Get(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
               .Returns(product);
            return uow.Object;
        }

        [Fact]
        public async Task Handle_BelowThreshold_IsLowStockTrue() {
            var uow = UnitOfWorkReturning(
                new Product { Id = 1, Title = "Low Book", StockQuantity = 2 });
            var handler = new GetInventoryStatusQueryHandler(uow);

            var result = await handler.Handle(
                new GetInventoryStatusQuery(1), CancellationToken.None);

            Assert.True(result.IsLowStock);
            Assert.Equal(2, result.StockQuantity);
        }

        [Fact]
        public async Task Handle_AboveThreshold_IsLowStockFalse() {
            var uow = UnitOfWorkReturning(
                new Product { Id = 2, Title = "Healthy Book", StockQuantity = 25 });
            var handler = new GetInventoryStatusQueryHandler(uow);

            var result = await handler.Handle(
                new GetInventoryStatusQuery(2), CancellationToken.None);

            Assert.False(result.IsLowStock);
        }
    }
}
