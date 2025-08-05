using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Bulky.DataAccess.Repository;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
		private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;

		}
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId) {
            RoleManagementVM roleManagementVM = new RoleManagementVM();
            roleManagementVM.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company");
            roleManagementVM.RoleList = _roleManager.Roles.Select(i => new SelectListItem { Text = i.Name, Value = i.Name });
            roleManagementVM.CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
            roleManagementVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
                .GetAwaiter().GetResult().FirstOrDefault();
            return View(roleManagementVM);
        }



		#region API CALLS
		[HttpPost]
		public IActionResult RoleManagement(RoleManagementVM roleManagementVM) {
            string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.ApplicationUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.ApplicationUser.Id);


            if(!(roleManagementVM.ApplicationUser.Role == oldRole)) {
                if(roleManagementVM.ApplicationUser.Role == SD.Role_Company) {
                    applicationUser.CompanyID = roleManagementVM.ApplicationUser.CompanyID;

                }
                if(oldRole == SD.Role_Company) {
                    applicationUser.CompanyID = null;

                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            } else {
                if(oldRole == SD.Role_Company && applicationUser.CompanyID != roleManagementVM.ApplicationUser.CompanyID) {
                    applicationUser.CompanyID= roleManagementVM.ApplicationUser.CompanyID;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    _unitOfWork.Save();
                }
            }
			return RedirectToAction("Index");
		}

		[HttpGet]
        public IActionResult GetAll() {
            List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();
			foreach(ApplicationUser user in objUserList) {

                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
                if(user.Company == null) { 
                    user.Company= new() { Name=""};
                }
            }
            return Json(new { data = objUserList });
        }

        [HttpPost]
		public IActionResult LockUnlock([FromBody]string id) {
            var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if(objFromDb == null) {
				return Json(new { success = false, message = "Error while Locking/Unlocking" });
			}
            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now) { 
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            } else {
                objFromDb.LockoutEnd= DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation successful" });
		}
		#endregion
	}
}
