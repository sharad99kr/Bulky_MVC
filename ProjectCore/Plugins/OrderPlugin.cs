using Bulky.DataAccess.Repository.IRepository;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ProjectCore.Plugins
{
    [Description("Provides real order status and order history for the signed-in customer")]
    public class OrderPlugin
    {
        private readonly IUnitOfWork _unitOfWork;

        //Resolved from the caller's claims and supplied at construction, never by the model.
        //A parameter the model cannot see is a parameter it cannot be talked into forging.
        private readonly string _userId;

        public OrderPlugin(IUnitOfWork unitOfWork, string userId) {
            _unitOfWork = unitOfWork;
            _userId = userId;
        }

        [KernelFunction("get_order_status")]
        [Description("Returns the current status of one of the signed-in customer's orders, by order ID")]
        public string GetOrderStatus(int orderId) {

            var order = _unitOfWork.OrderHeader.Get(o=> o.Id == orderId && o.ApplicationUserId == _userId);

            if(order == null) {
                //Same message whether the order is missing or belongs to someone else -
                //a distinct "not yours" reply would leak which IDs exist.
                return $"No order found with ID {orderId}.";
            } else {
                return $"Order #{orderId} - Status: {order.OrderStatus}, " +
                    $"Payment: {order.PaymentStatus}, " +
                    $"Placed: {order.OrderDate:d}.";
            }
        }

        [KernelFunction("get_recent_orders")]
        [Description("Returns the last N orders for the signed-in customer.")]
        public string GetRecentOrders(int count = 3) {
            count = Math.Min(count, 5); //hard cap - no method should return unlimited data/rows

            var orders = _unitOfWork.OrderHeader.GetAll(o => o.ApplicationUserId == _userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToList();

            if(orders.Count == 0) {
                return "No orders found on your account.";
            } else {
                var result = "Your recent orders:\n";
                foreach(var order in orders) {
                    result += $"- Order #{order.Id}: Status: {order.OrderStatus}, " +
                        $"Payment: {order.PaymentStatus}, " +
                        $"Placed: {order.OrderDate:d}\n";
                }
                return result;
            }


        }

    }
}
