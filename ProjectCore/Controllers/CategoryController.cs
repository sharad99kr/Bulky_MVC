using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;

namespace ProjectCore.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork) {
			//In constructor we are requesting for the implementation of ApplicationDbContext
			this.unitOfWork = unitOfWork;
        }
        public IActionResult Index() {
			List<Category> objCategoryList = unitOfWork.Category.GetAll().ToList();
			return View(objCategoryList);
        }

        public IActionResult Create() {
            return View(); //we can choose to pass an empty object or nothing. 
            //If nothing is passed, we automatically get an empty object of model defined in view
            //In above method we are explicitely passing the category list in the view
        }


        [HttpPost]
        public IActionResult Create(Category obj) {
            //we have two methods with same name create. The first method is sending/creating empty object of model
            //while this method is receiving same object with data in it due to post operation. Hence parameter in method signature
            if(obj.Name == obj.DisplayOrder.ToString()) {
                ModelState.AddModelError("Name", "The display order cannot be same as name"); //name links error to input field with name id in Create.cshtml file
                //ModelState.AddModelError is for custom error where we provide model key("Name") and error message
            }

            if(obj.Name.ToLower() == "test") {
                ModelState.AddModelError("", "Test is invalid value"); //if we do not provide first argument, error will be treated in global scope and will not fall under form validation
            }

            if(ModelState.IsValid) {
				unitOfWork.Category.Add(obj);
				unitOfWork.Save();
                TempData["success"] = "Category created successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
                return RedirectToAction("Index");
            }

            return View();
        }
        public IActionResult Edit(int? id) {
            if(id==null || id == 0) {
                return NotFound();
            }
			//Category categoryFromDb = _db.Categories.Find(id);
			//alternate ways to do a query on id. Using Linq
			//Category categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id);
			//Category categoryFromDb2 = _db.Categories.Where(u => u.Id == id).FirstOrDefault();

			Category categoryFromDb = unitOfWork.Category.Get(u=>u.Id==id);
			if(categoryFromDb == null) {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Category obj) {
           //we have to make sure obj has id which is set to 1(because we sent data with name id, if it was say categoryId, value will be 0), 
           //if it is set to 0, the db will create new field instead
           //of updating the current row. To solve this we create hidden id in Edit html file

            if(ModelState.IsValid) {
				unitOfWork.Category.Update(obj);
				unitOfWork.Save();
				return RedirectToAction("Index"); //this can be used to redirect pages in same controller. If we want to go to another
                //controller,we can pass the name of controller as second parameter along with action name
            }

            return View();
        }

        public IActionResult Delete(int? id) {
            if(id == null || id == 0) {
                return NotFound();
            }
            Category categoryFromDb = unitOfWork.Category.Get(u => u.Id == id);
			if(categoryFromDb == null) {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")] 
        public IActionResult DeletePOST(int? id) {//delete get and post cannot be of same name since parameter is same. It will cause confusion
            //So, we updated delete post method name and explicitely tell this end point action name is delete
            
            Category? obj = unitOfWork.Category.Get(u => u.Id == id);
			if(obj == null) {
                return NotFound();
            }
			unitOfWork.Category.Remove(obj);
			unitOfWork.Save();
			TempData["success"] = "Category deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
