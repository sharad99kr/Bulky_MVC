using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public ProductController(IUnitOfWork unitOfWork)
        {
            //In constructor we are requesting for the implementation of ApplicationDbContext
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Product> objProductList = unitOfWork.Product.GetAll().ToList();
            return View(objProductList);
        }

        public IActionResult Create()
        {
            return View(); //we can choose to pass an empty object or nothing. 
            //If nothing is passed, we automatically get an empty object of model defined in view
            //In above method we are explicitely passing the category list in the view
        }


        [HttpPost]
        public IActionResult Create(Product obj)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Add(obj);
                unitOfWork.Save();
                TempData["success"] = "Product created successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
                return RedirectToAction("Index");
            }

            return View();
        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //Product categoryFromDb = _db.Categories.Find(id);
            //alternate ways to do a query on id. Using Linq
            //Product categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id);
            //Product categoryFromDb2 = _db.Categories.Where(u => u.Id == id).FirstOrDefault();

            Product productFromDb = unitOfWork.Product.Get(u => u.Id == id);
            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Product obj)
        {
            //we have to make sure obj has id which is set to 1(because we sent data with name id, if it was say categoryId, value will be 0), 
            //if it is set to 0, the db will create new field instead
            //of updating the current row. To solve this we create hidden id in Edit html file

            if (ModelState.IsValid)
            {
                unitOfWork.Product.Update(obj);
                unitOfWork.Save();
                return RedirectToAction("Index"); //this can be used to redirect pages in same controller. If we want to go to another
                //controller,we can pass the name of controller as second parameter along with action name
            }

            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product productFromDb = unitOfWork.Product.Get(u => u.Id == id);
            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {//delete get and post cannot be of same name since parameter is same. It will cause confusion
         //So, we updated delete post method name and explicitely tell this end point action name is delete

            Product? obj = unitOfWork.Product.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            unitOfWork.Product.Remove(obj);
            unitOfWork.Save();
            TempData["success"] = "Product deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
