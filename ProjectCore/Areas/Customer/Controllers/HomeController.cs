using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using System.Security.Claims;
using Bulky.Utility;

namespace ProjectCore.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productsList = unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(productsList);
        }

		public IActionResult Details(int productId) {
            ShoppingCart cart = new() {
                Product = unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId
            };
			
			return View(cart);
		}

        [HttpPost]
        public IActionResult Details(ShoppingCart cart) {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //ClaimsIdentity contains the unique user id. Above lines are default ways provided to access the user ID 
            cart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = unitOfWork.ShoppingCart.Get(u=>u.ApplicationUserId == userId && 
                                                                u.ProductId == cart.ProductId);
            if(cartFromDb != null) {
                cartFromDb.Count += cart.Count;
                unitOfWork.ShoppingCart.Update(cartFromDb);
                unitOfWork.Save();
                //NOTE: By default entity framework core track any item retrieved from db and save them on modification even though we did not save it explicitely
                //here even if commit the update operation, the count will be updated in DB. The fix is tracked = false in IRepository
            } else {
                unitOfWork.ShoppingCart.Add(cart);
                unitOfWork.Save();
                //setting cart items count in session
                int countOfItemsInCart = unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, countOfItemsInCart);
            }
            TempData["success"] = "Cart updated successfully";
            
            return RedirectToAction(nameof(Index));// nameof gives list of all action methods in a class. This helps to avoid any error in naming
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
