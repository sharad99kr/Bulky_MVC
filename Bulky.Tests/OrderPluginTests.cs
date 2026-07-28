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
        private const string OwnerId = "user-owner";
        private const string OtherId = "user-other";

        //Mocks that actually evaluate the predicate the plugin passes in. Returning a
        //fixed row regardless of the filter would make the ownership tests below pass
        //even if the plugin dropped the user id check entirely.
        private static Mock<IUnitOfWork> UowOver(params OrderHeader[] rows) {
            var mockUow = new Mock<IUnitOfWork>();
            var mockOrderHeader = new Mock<IOrderHeaderRepository>();

            mockOrderHeader.Setup(r => r.Get(
                    It.IsAny<Expression<Func<OrderHeader, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<OrderHeader, bool>> filter, string _, bool __) =>
                    rows.FirstOrDefault(filter.Compile())!); //null is a valid "not found" here

            mockOrderHeader.Setup(r => r.GetAll(
                    It.IsAny<Expression<Func<OrderHeader, bool>>>(),
                    It.IsAny<string>()))
                .Returns((Expression<Func<OrderHeader, bool>> filter, string _) =>
                    filter == null ? rows : rows.Where(filter.Compile()));

            mockUow.Setup(u => u.OrderHeader).Returns(mockOrderHeader.Object);
            return mockUow;
        }

        private static OrderHeader Order(int id, string ownerId) => new OrderHeader {
            Id = id,
            ApplicationUserId = ownerId,
            OrderStatus = "Processing",
            PaymentStatus = "Approved",
            OrderDate = new DateTime(2026, 5, 1)
        };

        [Fact]
        public void GetOrderStatus_OrderExists_ReturnsStatusString() {

            var mockUow = UowOver(Order(42, OwnerId));

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);
            var result = plugin.GetOrderStatus(42);

            Assert.Contains("Order #42", result);
            Assert.Contains("Processing", result);

        }

        [Fact]
        public void GetOrderStatus_OrderNotFound_ReturnsNotFoundMessage() {

            var mockUow = UowOver();

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);
            var result = plugin.GetOrderStatus(9999);

            Assert.Contains("No order found", result);
        }

        [Fact]
        public void GetOrderStatus_OrderBelongsToAnotherUser_ReturnsNotFoundMessage() {

            //IDOR guard: the order exists, but not for this caller. The reply must be
            //byte-identical to the genuine not-found case so it can't be used to probe
            //which order IDs are real.
            var mockUow = UowOver(Order(42, OtherId));

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);
            var result = plugin.GetOrderStatus(42);

            Assert.Equal("No order found with ID 42.", result);
            Assert.DoesNotContain("Processing", result);
        }

        [Fact]
        public void GetRecentOrders_ReturnsOnlyCallersOrders() {

            var mockUow = UowOver(
                Order(1, OwnerId),
                Order(2, OtherId),
                Order(3, OwnerId),
                Order(4, OtherId));

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);
            var result = plugin.GetRecentOrders();

            Assert.Contains("Order #1", result);
            Assert.Contains("Order #3", result);
            Assert.DoesNotContain("Order #2", result);
            Assert.DoesNotContain("Order #4", result);
        }

        [Fact]
        public void GetRecentOrders_NoOrdersForCaller_ReturnsEmptyMessage() {

            var mockUow = UowOver(Order(1, OtherId), Order(2, OtherId));

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);
            var result = plugin.GetRecentOrders();

            Assert.Contains("No orders found", result);
            Assert.DoesNotContain("Order #", result);
        }

        [Fact]
        public void GetRecentOrders_CountCapAt5() {

            //verifies the hard cap of 5 is enforced regardless of input
            var mockUow = UowOver(Enumerable.Range(1, 10)
                .Select(i => Order(i, OwnerId))
                .ToArray());

            var plugin = new OrderPlugin(mockUow.Object, OwnerId);

            //Pass 100, should only call Take(5) internally
            var result = plugin.GetRecentOrders(100);

            //Can't assert Take(5) directly - assert result string count instead
            //Max 5 lines means max 5 "Order #" occurrences. Note the rendered lines are
            //prefixed with "- ", so match on Contains rather than StartsWith.
            var orderCount = result.Split('\n')
                .Count(line => line.Contains("Order #"));

            Assert.Equal(5, orderCount);
        }
    }
}
