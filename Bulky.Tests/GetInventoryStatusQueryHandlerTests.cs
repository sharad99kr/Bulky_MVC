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

namespace Bulky.Tests
{
    public class GetInventoryStatusQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ProductExists_ReturnsResult() {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockProductRepo = new Mock<IProductRepository>();
            mockProductRepo.Setup(r => r.Get(It.IsAny<Expression<Func<Product,bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
                .Returns(new Product { Id = 1, Title = "Test Book"});
            mockUnitOfWork.Setup(u => u.Product).Returns(mockProductRepo.Object);

            var handler = new GetInventoryStatusQueryHandler(mockUnitOfWork.Object);
            var result = await handler.Handle(new GetInventoryStatusQuery(1), CancellationToken.None);

            Assert.Equal(1, result.ProductId);
            Assert.Equal("Test Book", result.ProductName);
        }

        [Fact]
        public async Task Handle_ProductNotFound_ReturnsUnknown() {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockProductRepo = new Mock<IProductRepository>();
            mockProductRepo
                .Setup(r => r.Get(It.IsAny<Expression<Func<Product,bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
                .Returns((Product)null);
            mockUnitOfWork.Setup(u => u.Product).Returns(mockProductRepo.Object);
            
            var handler = new GetInventoryStatusQueryHandler(mockUnitOfWork.Object);
            var result = await handler.Handle(new GetInventoryStatusQuery(999), CancellationToken.None);
            Assert.Equal(999, result.ProductId);
            Assert.Equal("Product not found", result.ProductName);
        }
    }
}
