using Microsoft.AspNetCore.Mvc;
using ProjectCore.Data;
using ProjectCore.Models;

namespace ProjectCore.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db) {
            _db = db;
        }
        public IActionResult Index() {
            List<Category> objCategoryList = _db.Categories.ToList();
            return View(objCategoryList);
        }

        public IActionResult Create() {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category obj) {
            if(obj.Name == obj.DisplayOrder.ToString()) {
                ModelState.AddModelError("Name", "The display order cannot be same as name"); //name links error to input field with name id in Create.cshtml file
            }

            if(obj.Name.ToLower() == "test") {
                ModelState.AddModelError("", "Test is invalid value"); //if we do not provide first argument, error will be treated in global scope and will not fall under form validation
            }

            if(ModelState.IsValid) {
                _db.Categories.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Category created successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
                return RedirectToAction("Index");
            }

            return View();
        }
        public IActionResult Edit(int? id) {
            if(id==null || id == 0) {
                return NotFound();
            }
            Category categoryFromDb = _db.Categories.Find(id);
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
                _db.Categories.Update(obj);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Delete(int? id) {
            if(id == null || id == 0) {
                return NotFound();
            }
            Category categoryFromDb = _db.Categories.Find(id);
            if(categoryFromDb == null) {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id) {
            
            Category? obj=_db.Categories.Find(id);
            if(obj == null) {
                return NotFound();
            }
            _db.Categories.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Category deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
