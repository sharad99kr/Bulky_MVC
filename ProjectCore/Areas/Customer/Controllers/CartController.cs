using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Security.Claims;
using Session = Stripe.Checkout.Session;

namespace ProjectCore.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty] //this will automatically populate and update the values when changes are made on the view
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork) { 
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new() {
                 ShoppingCartList = _unitOfWork.ShoppingCart
                 .GetAll(u => u.ApplicationUserId == userId , includeProperties:"Product"),
                 OrderHeader = new OrderHeader()  
            };

            foreach(var cart in ShoppingCartVM.ShoppingCartList) {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            
            return View(ShoppingCartVM);
        }

        public IActionResult Summary() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new() {
                ShoppingCartList = _unitOfWork.ShoppingCart
                 .GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u=>u.Id==userId);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode=ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach(var cart in ShoppingCartVM.ShoppingCartList) {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPOST() {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart
                 .GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
			//we still need to populate list from db as some information might be lost in the view, we don't enter everything in the input field

			ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
			
			foreach(var cart in ShoppingCartVM.ShoppingCartList) {
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if(applicationUser.CompanyID.GetValueOrDefault() == 0) {
                //it is a regular customer 
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.PaymentStatusPending;
            } else {
                //it is a company user
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach(var cart in ShoppingCartVM.ShoppingCartList) { 
                OrderDetail orderDetail = new OrderDetail();
                orderDetail.ProductId = cart.ProductId;
                orderDetail.OrderHeaderId=ShoppingCartVM.OrderHeader.Id;
                orderDetail.Price = cart.Price;
                orderDetail.Count= cart.Count;
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if(applicationUser.CompanyID.GetValueOrDefault() == 0) {
                //it is a regular customer account and we need to capture payment
                //stripe logic
                var domain = "https://localhost:7081/";
                var options = new Stripe.Checkout.SessionCreateOptions {
                    SuccessUrl=domain+ $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain+"customer+cart+index",
                    LineItems=new List<SessionLineItemOptions>(),
                    Mode="payment",
                };

                foreach(var item in ShoppingCartVM.ShoppingCartList) {
                    var sessionLineItem = new SessionLineItemOptions {
                        PriceData=new SessionLineItemPriceDataOptions {
                            UnitAmount=(long)(item.Price*100), //$20.50=>2050
                            Currency="usd",
                            ProductData=new SessionLineItemPriceDataProductDataOptions {
                                Name=item.Product.Title
                            }
                        },
                        Quantity=item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                var service = new Stripe.Checkout.SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);

			}

			return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartVM.OrderHeader.Id});
		}

		public IActionResult OrderConfirmation(int id) {
            OrderHeader orderHeader=_unitOfWork.OrderHeader.Get(u=>u.Id==id, includeProperties: "ApplicationUser");
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment) {
                //this is an order by customer
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Get(orderHeader.SessionId);

                if(session.PaymentStatus.ToLower() == "paid") {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                                                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId)
                                                .ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
			return View(id);
		}

		public IActionResult Plus(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count++;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if(cartFromDb.Count <= 1) {
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            } else {
                cartFromDb.Count--;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart cart) { 
            if(cart.Count <= 50) {
                return cart.Product.Price;
            } else {
                if(cart.Count <= 100) {
                    return cart.Product.Price50;
                } else {
                    return cart.Product.Price100;
                }
            }
        }
    }
}
