using Bulky.DataAccess.Repository.IRepository;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ProjectCore.Plugins
{
    [Description("Provides real order status and order history for customers")]
    public class OrderPlugin
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderPlugin(IUnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        [KernelFunction("get_order_status")]
        [Description("Returns the current status of an order by order ID")]
        public string GetOrderStatus(int orderId) {
            
            var order = _unitOfWork.OrderHeader.Get(o=>o.Id == orderId);

            if(order == null) {
                return $"No order found with ID {orderId}.";
            } else {
                return $"Order #{orderId} - Status: {order.OrderStatus}, " +
                    $"Payment: {order.PaymentStatus}, " +
                    $"Placed: {order.OrderDate:d}.";
            }
        }

        [KernelFunction("get_recent_orders")]
        [Description("Returns the last N orders for a given user email.")]
        public string GetRecentOrders(string email, int count = 3) {
            count = Math.Min(count, 5); //hard cap - no method should return unlimited data/rows
            
            var orders = _unitOfWork.OrderHeader.GetAll(o => o.ApplicationUser.Email == email)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToList();
            
            if(orders.Count == 0) {
                return $"No orders found for email {email}.";
            } else {
                var result = $"Recent orders for {email}:\n";
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
