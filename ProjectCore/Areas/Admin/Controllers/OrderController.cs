using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace ProjectCore.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		[BindProperty]
		public OrderVM orderVM { get; set; }

		public OrderController(IUnitOfWork unitOfWork) {
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index() {
			return View();
		}

		public IActionResult Details(int orderId) {
			 orderVM = new OrderVM() {
				OrderHeader=_unitOfWork.OrderHeader.Get(u=>u.Id==orderId, includeProperties: "ApplicationUser"),
				OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product"),
			};
			return View(orderVM);
		}

		[HttpPost]
		[Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail() {
			var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name= orderVM.OrderHeader.Name; ;
            orderHeaderFromDb.PhoneNumber= orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress= orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City= orderVM.OrderHeader.City;
            orderHeaderFromDb.State= orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode= orderVM.OrderHeader.PostalCode;
			if(!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier)) {
                orderHeaderFromDb.Carrier= orderVM.OrderHeader.Carrier;
            }
            if(!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber)) {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }
			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["success"] = "Order Details Updated Successfully";
			return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing() {
            
            _unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder() {
			var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
			orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
			orderHeaderFromDb.Carrier= orderVM.OrderHeader.Carrier;
			orderHeaderFromDb.OrderStatus= SD.StatusShipped;
			orderHeaderFromDb.ShippingDate=DateTime.Now;
			if(orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment) {
				orderHeaderFromDb.PaymentDueDate=DateOnly.FromDateTime(DateTime.Now.AddDays(30));
			}
			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["success"] = "Order Shipped Successfully";
			return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
		}

		
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult CancelOrder() {
			var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
			
			if(orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved) {
				var options = new RefundCreateOptions {
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeaderFromDb.PaymentIntentId
				};

				var service=new RefundService();
				Refund refund=service.Create(options);
				_unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
			} else {
				//not giving refund
				_unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id,SD.StatusCancelled,SD.StatusCancelled);
			}
			
			_unitOfWork.Save();
			TempData["success"] = "Order Cancelled Successfully";
			return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
		}

		[ActionName("Details")]
		[HttpPost]
		public IActionResult Details_PAY_NOW() {
			orderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
			orderVM.OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderVM.OrderHeader.Id, includeProperties: "Product");

			//stripe logic
			var domain = Request.Scheme+"://"+Request.Host.Value+"/";
			var options = new Stripe.Checkout.SessionCreateOptions {
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderID={orderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId={orderVM.OrderHeader.Id}",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};

			foreach(var item in orderVM.OrderDetails) {
				var sessionLineItem = new SessionLineItemOptions {
					PriceData = new SessionLineItemPriceDataOptions {
						UnitAmount = (long)(item.Price * 100), //$20.50=>2050
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions {
							Name = item.Product.Title
						}
					},
					Quantity = item.Count
				};
				options.LineItems.Add(sessionLineItem);
			}
			var service = new Stripe.Checkout.SessionService();
			Session session = service.Create(options);
			_unitOfWork.OrderHeader.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}

		public IActionResult PaymentConfirmation(int orderHeaderId) {
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
			if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment) {
				//this is an order by company
				var service = new Stripe.Checkout.SessionService();
				Stripe.Checkout.Session session = service.Get(orderHeader.SessionId);

				if(session.PaymentStatus.ToLower() == "paid") {
					_unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.PaymentStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			
			return View(orderHeaderId);
		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status) {
			IEnumerable<OrderHeader> objOrderHeaders;
			
			if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee)) {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

			} else {
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId= claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId==userId,includeProperties: "ApplicationUser");
            }
			
			switch(status) {
				case "pending":
					objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusPending);
					break;
				case "inprocess":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
					break;
				case "completed":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
					break;
				case "approved":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
					break;
				default:
					break;
			}
			return Json(new { data = objOrderHeaders });
		}

		#endregion
	}
}
