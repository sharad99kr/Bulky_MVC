using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModels;

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
			//we need to pass category list as well at this location.
			//Step 1 would be to fetch all available categories with name and id
			//We will use projections in EF core to achieve it
			IEnumerable<SelectListItem> CategoryList = unitOfWork
														.Category
														.GetAll()
														.Select(u => new SelectListItem {
															Text = u.Name,
															Value = u.Id.ToString()
														});
			//step2 will be to pass this object(CategoryList) to the view
			//we can pass data that are not in model using 3 ways : 1.ViewBag 2.ViewData 3.TempData
			//1. Data passed as ViewBag persists only for current http request and data is lost if there is redirection
			//2. ViewData is similar to ViewBag but it's value must be type cast before use
            //3. TempData uses session to store data. It's value also needs to be type cast before use. It can be used for error/validations messages as well
			//Usually we avoid using them and use ViewModels instead. This is because views are tightly bound wwith models and it is hard to identify where the extra data is coming from.
            //ViewBag.CategoryList = CategoryList;
			//ViewData["CategoryList"] = CategoryList;

            ProductVM productVM = new() {
                CategoryList = CategoryList,
                Product = new Product()
            };
			return View(productVM); 
        }


        [HttpPost]
        public IActionResult Create(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Add(productVM.Product);
                unitOfWork.Save();
                TempData["success"] = "Product created successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
                return RedirectToAction("Index");
            } else {
				productVM.CategoryList = unitOfWork
                                        .Category
                                        .GetAll()
                                        .Select(u => new SelectListItem {
                                            Text = u.Name,
                                            Value = u.Id.ToString()
                                        });
				return View(productVM);
			}
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
