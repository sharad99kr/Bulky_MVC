using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Moq;
using ProjectCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests
{
    public class OrderPluginTests
    {
        [Fact]
        public void GetOrderStatus_OrderExists_ReturnsStatusString() {
            
            var mockUow = new Mock<IUnitOfWork>();
            var mockOrderHeader = new Mock<IOrderHeaderRepository>();
            
            mockOrderHeader.Setup(r => r.Get(It.IsAny<Expression<Func<OrderHeader, bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
                .Returns(new OrderHeader {
                    Id = 42,
                    OrderStatus = "Processing",
                    PaymentStatus = "Approved",
                    OrderDate = new DateTime(2026, 5, 1)
                });
            mockUow.Setup(u=>u.OrderHeader).Returns(mockOrderHeader.Object);

            var plugin = new OrderPlugin(mockUow.Object);
            var result = plugin.GetOrderStatus(42);

            Assert.Contains("Order #42", result);
            Assert.Contains("Processing", result);

        }

        [Fact]
        public void GetOrderStatus_OrderNotFound_ReturnsNotFoundMessage() {
            
            var mockUow = new Mock<IUnitOfWork>();
            var mockOrderHeader = new Mock<IOrderHeaderRepository>();
            
            mockOrderHeader.Setup(r => r.Get(
                It.IsAny<Expression<Func<OrderHeader, bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
                .Returns( (OrderHeader) null);

            mockUow.Setup(u=>u.OrderHeader)
                .Returns(mockOrderHeader.Object);

            var plugin = new OrderPlugin (mockUow.Object);
            var result = plugin.GetOrderStatus(9999);

            Assert.Contains("No order found", result);
        }

        [Fact]
        public void GetRecentOrders_CountCapAt5() {

            //verifies the hard cap of 5 is enforced regardless of input
            var mockUow = new Mock<IUnitOfWork>();
            var mockOrderHeader = new Mock<IOrderHeaderRepository>();

            mockOrderHeader.Setup(r => r.GetAll(
                It.IsAny<Expression<Func<OrderHeader,bool>>>(),
                It.IsAny<string>()))
                .Returns(Enumerable.Range(1,10)
                .Select(i => new OrderHeader { Id = i }).AsQueryable());
            mockUow.Setup (u=>u.OrderHeader).Returns(mockOrderHeader.Object);

            var plugin = new OrderPlugin(mockUow.Object);

            //Pass 100, should only call Take(5) internally
            var result = plugin.GetRecentOrders("test@example.com", 100);

            //Can't assert Take(5) directly - assert result string count instead
            //Max 5 lines means max 5 "Order #" occurrences
            var orderCount = result.Split('\n')
                .Count(line => line.StartsWith("Order #"));

            Assert.True( orderCount <= 5);
        }
    }
}
