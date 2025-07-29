using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectCore.Areas.Customer.Controllers;
using System.Security.Claims;

namespace ProjectCore.ViewComponents
{
    //Note:It is mandatory to append 'ViewComponent' to the end of class name along with inherit of ':ViewComponent' for it to work
    //ALso its view has to follow strict structure: Views->Shared->Components(new)->ShoppingCart(new)->Default.cshtml
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent( IUnitOfWork unitOfWork) {
            this._unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(claim != null) {
                if(HttpContext.Session.GetInt32(SD.SessionCart) == null) {
                    int countOfItemsInCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count();
                    HttpContext.Session.SetInt32(SD.SessionCart, countOfItemsInCart);
                }
                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            } else {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }

    
}
