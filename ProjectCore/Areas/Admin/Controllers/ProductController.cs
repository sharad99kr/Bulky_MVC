using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModels;
using Microsoft.IdentityModel.Tokens;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Humanizer;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment; //this is to access www root folder for images
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            //In constructor we are requesting for the implementation of ApplicationDbContext
            this.unitOfWork = unitOfWork;
            this.webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProductList = unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            
			return View(objProductList);
        }

        public IActionResult Upsert(int? id) //update+insert
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
            if(id == null || id == 0) {
                //create
                return View(productVM);
            } else {
                //update
                productVM.Product = unitOfWork.Product.Get(u=>u.Id==id, includeProperties:"ProductImages");
				return View(productVM);
			}
			
        }


        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
				if(productVM.Product.Id == 0) {
					//No product Id(primary key) means it is new product
					unitOfWork.Product.Add(productVM.Product);
				} else {
					unitOfWork.Product.Update(productVM.Product);
				}
				unitOfWork.Save();

				string wwwRootPath = webHostEnvironment.WebRootPath;
                if(files != null) {
                    foreach(IFormFile file in files) { 
                        string fileName=Guid.NewGuid().ToString()+Path.GetExtension(file.FileName); 
                        string productPath=@"images\product\product-"+productVM.Product.Id;
                        string filePath=Path.Combine(wwwRootPath, productPath);

                        if(!Directory.Exists(filePath)) { 
                            Directory.CreateDirectory(filePath);
                        }

                        using(var filestream = new FileStream(Path.Combine(filePath, fileName), FileMode.Create)) { 
                            file.CopyTo(filestream);
                        }

                        ProductImage productImage = new() {
                            ImageUrl=@"\"+productPath+@"\"+fileName,
                            ProductId=productVM.Product.Id,
                        };

                        if(productVM.Product.ProductImages == null) {
							productVM.Product.ProductImages=new List<ProductImage>();
						}
                        productVM.Product.ProductImages.Add(productImage);
                    }
                    unitOfWork.Product.Update(productVM.Product);
					unitOfWork.Save();
				}

                TempData["success"] = "Product created /updated successfully!"; //temp data is used to preserve info until next load of page. If page is refreshed, data is loast
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

        public IActionResult DeleteImage(int imageId) {
            var imageToBeDeleted = unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null) {
                if(!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl)) {
                    var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath,
                        imageToBeDeleted.ImageUrl.TrimStart('\\'));
                    if(System.IO.File.Exists(oldImagePath)) {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                unitOfWork.ProductImage.Remove(imageToBeDeleted);
                unitOfWork.Save();
				TempData["success"] = "Deleted successfully!";
			}
            return RedirectToAction(nameof(Upsert),new {id=productId});
        }

		#region API CALLS
		[HttpGet]
        public IActionResult GetAll() {
            List<Product> objProductList = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
		public IActionResult Delete(int? id) {

            var productToBeDeleted = unitOfWork.Product.Get(u=>u.Id == id);
            if (productToBeDeleted == null) {
                return Json(new {success = false,message = "Error while deleting"});
            }

			string productPath = Path.Combine("images", "product", "product-" + id);
			string finalPath=Path.Combine(webHostEnvironment.WebRootPath, productPath);

            if(Directory.Exists(finalPath)) {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (var filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }

            unitOfWork.Product.Remove(productToBeDeleted);
            unitOfWork.Save();

			return Json(new { success = true, message = "Delete Successful" });
		}
		#endregion
	}
}
