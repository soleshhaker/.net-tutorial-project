using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    [Route("[area]/[controller]")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(objProductList);
        }

        [HttpGet("Upsert/{id?}")]
        public IActionResult Upsert(int? id)
        {
            ProductViewModel productView = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productView);
            }
            else
            {
                //update
                productView.Product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id, includeProperties:"ProductImages");
                return View(productView);
            }
        }

        //POST
        [HttpPost("Upsert")]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel productViewModel, List<IFormFile>? files)
        {
            if (ModelState.IsValid)
            {
                if (productViewModel.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productViewModel.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productViewModel.Product);

                }
                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if(files != null)
                {
                    foreach(IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productViewModel.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath,productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productViewModel.Product.Id,
                        };

                        if (productViewModel.Product.ProductImages == null)
                            productViewModel.Product.ProductImages = new();

                        productViewModel.Product.ProductImages.Add(productImage);
                    }
                    _unitOfWork.Product.Update(productViewModel.Product);
                    _unitOfWork.Save();
                }

              
                TempData["success"] = "Product created/updates succesfully";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productViewModel.CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                });

                return View(productViewModel);
            }
        }

        [HttpPost("DeleteImage")]
        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.GetFirstOrDefault(x => x.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                        imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }
            return RedirectToAction(nameof(Upsert), new { id = productId });
        }
        #region API CALLS

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = objProductList });
        }
        [HttpDelete("Delete/{id?}")]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new {success = false, message = "Error while deleting"});
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
