using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            //In constructor we are requesting for the implementation of ApplicationDbContext
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Company> objCompanyList = unitOfWork.Company.GetAll().ToList();
            
			return View(objCompanyList);
        }

        public IActionResult Upsert(int? id) //update+insert
        {

            if(id == null || id == 0) {
                //create
                return View(new Company());
            } else {
                //update
                Company companyObj = unitOfWork.Company.Get(u=>u.Id==id);
				return View(companyObj);
			}
			
        }


        [HttpPost]
        public IActionResult Upsert(Company companyObj)
        {
            if (ModelState.IsValid)
            {
                if(companyObj.Id == 0) {
                    //No product Id(primary key) means it is new product
                    unitOfWork.Company.Add(companyObj);
                } else {
					unitOfWork.Company.Update(companyObj);
                }

                unitOfWork.Save();
                TempData["success"] = "Company created successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
                return RedirectToAction("Index");
            } else {
				return View(companyObj);
			}
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {//delete get and post cannot be of same name since parameter is same. It will cause confusion
         //So, we updated delete post method name and explicitely tell this end point action name is delete

            Company? obj = unitOfWork.Company.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            unitOfWork.Company.Remove(obj);
            unitOfWork.Save();
            TempData["success"] = "Company deleted successfully!";
            return RedirectToAction("Index");
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll() {
            List<Company> objCompanyList = unitOfWork.Company.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
		public IActionResult Delete(int? id) {

            var companyToBeDeleted = unitOfWork.Company.Get(u=>u.Id == id);
            if (companyToBeDeleted == null) {
                return Json(new {success = false,message = "Error while deleting"});
            }

            unitOfWork.Company.Remove(companyToBeDeleted);
            unitOfWork.Save();

			return Json(new { success = true, message = "Delete Successful" });
		}
		#endregion
	}
}
