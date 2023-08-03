using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

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
            try
            {
                IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category");

                // Log the number of products returned
                int productCount = objProductList.Count();
                Log.Information("Returned {ProductCount} product(s) in the Index page at {Timestamp}", productCount, DateTime.Now);

                return View(objProductList);
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while processing the Index page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the Index page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpGet("Upsert/{id?}")]
        public IActionResult Upsert(int? id)
        {
            try
            {
                ProductViewModel productView = new ProductViewModel
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
                    // Create
                    Log.Information("User accessed the Upsert page to create a new product at {Timestamp}", DateTime.Now);
                    return View(productView);
                }
                else
                {
                    // Update
                    productView.Product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id, includeProperties: "ProductImages");

                    // Log the product being updated
                    Log.Information("User accessed the Upsert page to update product with ID {ProductId} at {Timestamp}", id, DateTime.Now);

                    return View(productView);
                }
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while processing the Upsert page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the Upsert page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost("Upsert")]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel productViewModel, List<IFormFile>? files)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (productViewModel.Product.Id == 0)
                    {
                        _unitOfWork.Product.Add(productViewModel.Product);
                        Log.Information("Product created. Product ID: {ProductId} at {Timestamp}", productViewModel.Product.Id, DateTime.Now);
                    }
                    else
                    {
                        _unitOfWork.Product.Update(productViewModel.Product);
                        Log.Information("Product updated. Product ID: {ProductId} at {Timestamp}", productViewModel.Product.Id, DateTime.Now);
                    }
                    _unitOfWork.Save();

                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    if (files != null)
                    {
                        foreach (IFormFile file in files)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string productPath = @"images\products\product-" + productViewModel.Product.Id;
                            string finalPath = Path.Combine(wwwRootPath, productPath);

                            if (!Directory.Exists(finalPath))
                            {
                                Directory.CreateDirectory(finalPath);
                            }

                            using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            ProductImage productImage = new ProductImage
                            {
                                ImageUrl = @"\" + productPath + @"\" + fileName,
                                ProductId = productViewModel.Product.Id,
                            };

                            if (productViewModel.Product.ProductImages == null)
                                productViewModel.Product.ProductImages = new List<ProductImage>();

                            productViewModel.Product.ProductImages.Add(productImage);
                        }
                        _unitOfWork.Product.Update(productViewModel.Product);
                        _unitOfWork.Save();
                    }

                    // Log the success message and the ID of the product created/updated
                    string action = productViewModel.Product.Id == 0 ? "created" : "updated";
                    Log.Information("Product {Action} successfully. Product ID: {ProductId} at {Timestamp}", action, productViewModel.Product.Id, DateTime.Now);

                    TempData["success"] = "Product created/updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    productViewModel.CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString(),
                    });

                    // Log the validation failure
                    Log.Warning("Validation failed while processing the Upsert page at {Timestamp}", DateTime.Now);

                    return View(productViewModel);
                }
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while processing the Upsert page at {Timestamp}", DateTime.Now);

                TempData["error"] = "An error occurred while processing the Upsert page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }



        [HttpPost("DeleteImage")]
        public IActionResult DeleteImage(int imageId)
        {
            ProductImage imageToBeDeleted = null;
            try
            {
                imageToBeDeleted = _unitOfWork.ProductImage.GetFirstOrDefault(x => x.Id == imageId);
                int productId = imageToBeDeleted.ProductId;
                if (imageToBeDeleted != null)
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

                    // Log the successful deletion and the Image ID
                    Log.Information("Image deleted successfully. Image ID: {ImageId} at {Timestamp}", imageId, DateTime.Now);

                    TempData["success"] = "Deleted successfully";
                }

                return RedirectToAction(nameof(Upsert), new { id = productId });
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while deleting the image with ID {ImageId} at {Timestamp}", imageId, DateTime.Now);

                TempData["error"] = "An error occurred while deleting the image. Please try again later.";
                return RedirectToAction(nameof(Upsert), new { id = imageToBeDeleted?.ProductId });
            }
        }

        #region API CALLS

        [HttpGet("GetAll")]
        [Produces("application/json")]
        public IActionResult GetAll()
        {
            try
            {
                IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category");

                // Log the successful retrieval of all products
                Log.Information("Successfully retrieved all products at {Timestamp}", DateTime.Now);

                return Json(new { data = objProductList });
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while retrieving all products at {Timestamp}", DateTime.Now);

                return Json(new { data = new List<Product>() });
            }
        }

        [HttpDelete("Delete/{id?}")]
        public IActionResult Delete(int? id)
        {
            try
            {
                var productToBeDeleted = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);
                if (productToBeDeleted == null)
                {
                    return Json(new { success = false, message = "Error while deleting" });
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

                // Log the successful deletion and the Product ID
                Log.Information("Product deleted successfully. Product ID: {ProductId} at {Timestamp}", id, DateTime.Now);

                return Json(new { success = true, message = "Delete Successful" });
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Log.Error(ex, "An error occurred while deleting the product with ID {ProductId} at {Timestamp}", id, DateTime.Now);

                return Json(new { success = false, message = "An error occurred while deleting the product. Please try again later." });
            }
        }

        #endregion
    }
}
