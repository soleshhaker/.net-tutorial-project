using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll();
            return View(objProductList);
        }

        //GET
        public IActionResult Create()
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
            return View(productView);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductViewModel productViewModel)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Add(productViewModel.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product created succesfully";
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
        //EDIT
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);
            if (productFromDb == null) return NotFound();

            return View(productFromDb);
        }

        //EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product obj)
        {
            if (!ModelState.IsValid) return View(obj);

            _unitOfWork.Product.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Product edited succesfully";
            return RedirectToAction(nameof(Index));
        }
        //DELETE
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);
            if (productFromDb == null) return NotFound();

            return View(productFromDb);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);
            if (productFromDb == null) return NotFound();

            _unitOfWork.Product.Remove(productFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Product deleted succesfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
