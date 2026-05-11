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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IConfiguration configuration) {
            this.unitOfWork = unitOfWork;
            this.webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        private BlobContainerClient GetBlobContainerClient() {
            string connectionString = _configuration["AzureStorage:ConnectionString"];
            string containerName = _configuration["AzureStorage:ContainerName"];
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists(PublicAccessType.Blob);
            return containerClient;
        }

        public IActionResult Index() {
            List<Product> objProductList = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProductList);
        }

        public IActionResult Upsert(int? id) {
            IEnumerable<SelectListItem> CategoryList = unitOfWork
                .Category
                .GetAll()
                .Select(u => new SelectListItem {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

            ProductVM productVM = new() {
                CategoryList = CategoryList,
                Product = new Product()
            };

            if(id == null || id == 0) {
                return View(productVM);
            } else {
                productVM.Product = unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files) {
            if(ModelState.IsValid) {
                if(productVM.Product.Id == 0) {
                    unitOfWork.Product.Add(productVM.Product);
                } else {
                    unitOfWork.Product.Update(productVM.Product);
                }
                unitOfWork.Save();

                if(files != null) {
                    var containerClient = GetBlobContainerClient();

                    foreach(IFormFile file in files) {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string blobPath = $"product/product-{productVM.Product.Id}/{fileName}";
                        BlobClient blobClient = containerClient.GetBlobClient(blobPath);

                        using(var stream = file.OpenReadStream()) {
                            blobClient.Upload(stream, overwrite: true);
                        }

                        ProductImage productImage = new() {
                            ImageUrl = blobClient.Uri.ToString(),
                            ProductId = productVM.Product.Id,
                        };

                        if(productVM.Product.ProductImages == null) {
                            productVM.Product.ProductImages = new List<ProductImage>();
                        }
                        productVM.Product.ProductImages.Add(productImage);
                    }

                    unitOfWork.Product.Update(productVM.Product);
                    unitOfWork.Save();
                }

                TempData["success"] = "Product created/updated successfully!";
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
        public IActionResult DeletePOST(int? id) {
            Product? obj = unitOfWork.Product.Get(u => u.Id == id);
            if(obj == null) {
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
                    // Only delete from blob if it's a blob URL
                    if(imageToBeDeleted.ImageUrl.StartsWith("https://")) {
                        var containerClient = GetBlobContainerClient();
                        Uri uri = new Uri(imageToBeDeleted.ImageUrl);
                        string blobName = string.Join("", uri.Segments.Skip(2));
                        BlobClient blobClient = containerClient.GetBlobClient(blobName);
                        blobClient.DeleteIfExists();
                    }
                    // Old local path images are just removed from DB, file is already gone after redeployment
                }

                unitOfWork.ProductImage.Remove(imageToBeDeleted);
                unitOfWork.Save();
                TempData["success"] = "Deleted successfully!";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll() {
            List<Product> objProductList = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id) {
            var productToBeDeleted = unitOfWork.Product.Get(u => u.Id == id);
            if(productToBeDeleted == null) {
                return Json(new { success = false, message = "Error while deleting" });
            }

            var containerClient = GetBlobContainerClient();
            string prefix = $"product/product-{id}/";
            await foreach(var blobItem in containerClient.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.All, prefix: prefix, cancellationToken: CancellationToken.None)) {
                containerClient.GetBlobClient(blobItem.Name).DeleteIfExists();
            }

            unitOfWork.Product.Remove(productToBeDeleted);
            unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}